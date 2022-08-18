using System;
using System.Collections.Generic;
using System.Linq;
using EvilDICOM.Core;
using EvilDICOM.Core.Helpers;
using EvilDICOM.Core.Image;
using System.IO;
using System.Windows.Forms;
using System.Reflection;

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
        string writePath;
        public Run() { }
        public void Go()
        {
            if (loadConfiguration(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\configuration\\fixCT_config.ini")) { return; } 
            writePath = initialPath;
            //get all directories in CT DICOM dump directory (each folder represents a different patient)
            List<string> dirs = Directory.EnumerateDirectories(initialPath).ToList();
            if (dirs.Count == 0) { Console.WriteLine("No directories found! Exiting"); Console.ReadLine(); return; }
            List<Tuple<string,string>> names = new List<Tuple<string,string>> { };
            int count = 0;
            Console.WriteLine("Select a patient CT dataset to process.");
            Console.WriteLine("Enter 'n' to use the UI to select a folder. Enter 'q' to quit.");
            foreach (string d in dirs)
            {
                //get the names of each patient whose CT data is in the CT DICOM dump directory
                DICOMObject dcmObj = DICOMObject.Read(Directory.EnumerateFiles(d).First());
                names.Add(new Tuple<string,string>(dcmObj.FindFirst(TagHelper.PatientName).DData as string, d));
                //print the patient names in the console as (count - patient name)
                Console.WriteLine(String.Format("{0} - {1}", count, names.ElementAt(count)));
                count++;
            }
            Console.WriteLine("");
            Console.Write("Selection: ");
            bool isError = true;
            //have the user select a patient dataset to process
            while(isError)
            {
                string line = Console.ReadLine();
                if (int.TryParse(line, out int option))
                {
                    //a number was entered (corresponds to count)
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
                    //user would prefer to browse to the patient folder themselves
                    FolderBrowserDialog fbd = new FolderBrowserDialog();
                    fbd.SelectedPath = initialPath;
                    DialogResult result = fbd.ShowDialog();
                    if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
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
            //code to actually modify the CT data
            //check to make sure the CT data hasn't been modified previously. If so, remove the directory containing the modified CT data
            if (Directory.Exists(writePath)) foreach (FileInfo f in new DirectoryInfo(writePath).GetFiles()) f.Delete();
            else Directory.CreateDirectory(writePath);

            //use EVILDICOM to process the CT data
            EvilDICOM.Anonymization.Settings.AnonymizationSettings settings = EvilDICOM.Anonymization.Settings.AnonymizationSettings.Default;
            settings.DoAnonymizeNames = false;
            settings.DoAnonymizeStudyIDs = true;
            settings.DoRemovePrivateTags = false; // has to be set to false otherwise will give an error (collection was modified during anonymization)
            settings.DoAnonymizeUIDs = true;
            settings.FirstName = (DICOMObject.Read(files.First()).FindFirst(TagHelper.PatientName) as EvilDICOM.Core.Element.PersonName).FirstName;
            settings.LastName = (DICOMObject.Read(files.First()).FindFirst(TagHelper.PatientName) as EvilDICOM.Core.Element.PersonName).LastName;
            settings.Id = DICOMObject.Read(files.First()).FindFirst(TagHelper.PatientID).DData as string;
            //you need to anonymize the CT data (only anonymize the study and UIDs so it will break the dicom link so Eclipse will read the two CTs as separate scans)
            //Otherwise Eclipse will interpret the two CTs and a single dataset and import them as a single scan
            EvilDICOM.Anonymization.AnonymizationQueue anon = EvilDICOM.Anonymization.AnonymizationQueue.BuildQueue(settings, files.ToList());
            foreach (string file in files.ToList())
            {
                Console.WriteLine(file);
                //read the DICOM image data
                DICOMObject o = DICOMObject.Read(file);
                //get the pixel stream
                PixelStream pixels = EvilDICOM.Core.Extensions.DICOMObjectExtensions.GetPixelStream(o);
                //get the underlying pixel data as int16 type
                short[] pixelVals = pixels.GetValues16(true);
                for (int i = 0; i < pixelVals.Length; i++)
                {
                    // if the pixel value is non-negative, decrease the pixel value by 2^15, otherwise set the pixel value to -2^15
                    if (pixelVals[i] >= 0) pixelVals[i] = (short)(pixelVals[i] - Math.Pow(2, 15));
                    else pixelVals[i] = (short)-Math.Pow(2, 15);
                }
                //convert the pixel values back to int32 (annoying and generally unnecessary, but the setvalues16 method of pixelstream requires int32[] as the data type)
                int[] test = Array.ConvertAll(pixelVals, x => (int)x);
                pixels.SetValues16(test, true);
                //set the pixel stream for the dicom object
                EvilDICOM.Core.Extensions.DICOMObjectExtensions.SetPixelStream(o, pixels.ToArray());
                //important, adjust the rescale intercept (used to scale the pixel value to HU)
                var stuff = o.FindFirst(TagHelper.RescaleIntercept) as EvilDICOM.Core.Element.DecimalString;
                double intercept = (double)stuff.DData;
                //increase the current intercept value by 2^15 to compensate for the -2^15 offset that was introduced in the for loop above (the slope will remain the same)
                stuff.DData = Math.Pow(2, 15) + intercept;
                //update the rescale intercept in the dicom header
                o.ReplaceOrAdd(stuff);
                //anonymize the new dicom file
                anon.Anonymize(o);
                //write the new dicom image in a folder created inside the patient folder that was located inside the CT dicom dump folder
                o.Write(writePath + "\\" + file.Substring(file.LastIndexOf("\\"), file.Length - file.LastIndexOf("\\")));
            }
        }

        private bool loadConfiguration(string file)
        {
            try
            {
                using (StreamReader reader = new StreamReader(file))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrEmpty(line) && line.Substring(0, 1) != "%")
                        {
                            //default configuration parameters
                            string parameter = line.Substring(0, line.IndexOf("="));
                            string value = line.Substring(line.IndexOf("=") + 1, line.Length - line.IndexOf("=") - 1);
                            if(parameter == "CT DICOM path")
                            {
                                //check to make sure CT DICOM dump directory exists
                                if (Directory.Exists(value)) initialPath = value;
                                else { Console.WriteLine(String.Format("Error! {0} does not exist!", initialPath)); Console.WriteLine("Exiting!"); Console.ReadLine(); return true; } 
                            }
                        }
                    }
                }
                return false;
            }
            catch (Exception e) { MessageBox.Show(String.Format("Could not load configuration file because {0}! Exiting!", e.Message)); return true; }
        }
    }
}
