using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EvilDICOM.Core;
using EvilDICOM.Core.Helpers;
using EvilDICOM.Core.Image;
using System.Windows;
using System.IO;
using Microsoft.Win32;
using System.Windows.Forms;


namespace fixCT
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Run r = new Run();
            r.Go();
        }
    }

    class Run
    {
        //string initialPath = @"C:\Users\Eric Simiele\Documents\Rutgers\dicom";
        string initialPath = @"\\cdcpvwavafile01\va_data$\DICOM\NBR";
        //string writePath = @"C:\Users\Eric Simiele\Documents\Rutgers\dcm_copy";
        //string writePath = @"\\vfs0006\RadData\oncology\ESimiele\tmp";
        string writePath = @"\\cdcpvwavafile01\va_data$\DICOM\NBR";
        public Run() { }
        public void Go()
        {
            if (!Directory.Exists(initialPath)) { Console.WriteLine(String.Format("Error! {0} does not exist!", initialPath)); Console.WriteLine("Exiting!"); Console.ReadLine(); return; }
            List<string> dirs = Directory.EnumerateDirectories(initialPath).ToList();
            if (dirs.Count == 0) { Console.WriteLine("No directories found! Exiting"); Console.ReadLine(); return; }
            List<Tuple<string,string>> names = new List<Tuple<string,string>> { };
            int count = 0;
            Console.WriteLine("Select a patient CT dataset to process.");
            Console.WriteLine("Enter 'n' to use the UI to select a folder. Enter 'q' to quit.");
            foreach (string d in dirs)
            {
                DICOMObject dcmObj = DICOMObject.Read(Directory.EnumerateFiles(d).First());
                names.Add(new Tuple<string,string>(dcmObj.FindFirst(TagHelper.PatientName).DData as string, d));
                Console.WriteLine(String.Format("{0} - {1}", count, names.ElementAt(count)));
                count++;
            }
            Console.WriteLine("");
            Console.Write("Selection: ");
            bool isError = true;
            while(isError)
            {
                string line = Console.ReadLine();
                if (int.TryParse(line, out int option))
                {
                    if (option + 1 <= names.Count())
                    {
                        //writePath += "\\" + names.ElementAt(option).Item1;
                        writePath = names.ElementAt(option).Item2 + "\\" + names.ElementAt(option).Item1;
                        Console.WriteLine(writePath);
                        processDCMfiles(Directory.EnumerateFiles(names.ElementAt(option).Item2));
                        isError = false;
                    }
                    else Console.WriteLine("Input not recognized! Please try again!");
                }
                else if (line == "n")
                {
                    System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
                    fbd.SelectedPath = initialPath;
                    System.Windows.Forms.DialogResult result = fbd.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                    {
                        writePath = fbd.SelectedPath + "\\" + DICOMObject.Read(Directory.EnumerateFiles(fbd.SelectedPath).First()).FindFirst(TagHelper.PatientName).DData as string;
                        processDCMfiles(Directory.EnumerateFiles(fbd.SelectedPath, "*.dcm"));
                    }
                    isError = false;
                }
                else if (line == "q") isError = false;
                else Console.WriteLine("Input not recognized! Please try again!");
            }
            Console.WriteLine("");
            Console.WriteLine("Done. Hit enter to exit.");
            Console.ReadLine();
        }

        private void processDCMfiles(IEnumerable<string> files)
        {
            if (Directory.Exists(writePath)) foreach (FileInfo f in new DirectoryInfo(writePath).GetFiles()) f.Delete();
            else Directory.CreateDirectory(writePath);

            EvilDICOM.Anonymization.Settings.AnonymizationSettings settings = EvilDICOM.Anonymization.Settings.AnonymizationSettings.Default;
            settings.DoAnonymizeNames = false;
            settings.DoAnonymizeStudyIDs = true;
            settings.DoRemovePrivateTags = false; // has to be set to false otherwise will give an error (collection was modified during anonymization)
            settings.DoAnonymizeUIDs = true;
            settings.FirstName = (DICOMObject.Read(files.First()).FindFirst(TagHelper.PatientName) as EvilDICOM.Core.Element.PersonName).FirstName;
            settings.LastName = (DICOMObject.Read(files.First()).FindFirst(TagHelper.PatientName) as EvilDICOM.Core.Element.PersonName).LastName;
            settings.Id = DICOMObject.Read(files.First()).FindFirst(TagHelper.PatientID).DData as string;
            EvilDICOM.Anonymization.AnonymizationQueue anon = EvilDICOM.Anonymization.AnonymizationQueue.BuildQueue(settings, files.ToList());
            foreach (string file in files.ToList())
            {
                Console.WriteLine(file);
                DICOMObject o = DICOMObject.Read(file);
                EvilDICOM.Core.Image.PixelStream pixels = EvilDICOM.Core.Extensions.DICOMObjectExtensions.GetPixelStream(o);
                short[] pixelVals = pixels.GetValues16(true);
                for (int i = 0; i < pixelVals.Length; i++)
                {
                    if (pixelVals[i] >= 0) pixelVals[i] = (short)(pixelVals[i] - System.Math.Pow(2, 15));
                    else pixelVals[i] = (short)-System.Math.Pow(2, 15);
                }
                int[] test = Array.ConvertAll(pixelVals, x => (int)x);
                pixels.SetValues16(test, true);
                EvilDICOM.Core.Extensions.DICOMObjectExtensions.SetPixelStream(o, pixels.ToArray());
                EvilDICOM.Core.Image.PixelStream pix = EvilDICOM.Core.Extensions.DICOMObjectExtensions.GetPixelStream(o);
                short[] pixVals = pix.GetValues16(true);
                var stuff = o.FindFirst(TagHelper.RescaleIntercept) as EvilDICOM.Core.Element.DecimalString;
                double intercept = (double)stuff.DData;
                stuff.DData = System.Math.Pow(2, 15) + intercept;
                o.ReplaceOrAdd(stuff);
                anon.Anonymize(o);
                o.Write(writePath + "\\" + file.Substring(file.LastIndexOf("\\"), file.Length - file.LastIndexOf("\\")));
            }
        }
    }
}
