using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Windows.Forms;
using VMS.TPS.Common.Model.API;

namespace VMS.TPS
{
    public class Script
    {
        public void Execute(ScriptContext context)
        {
            try
            {
                Process.Start(AppExePath());
            }
            catch (Exception e) { MessageBox.Show(e.Message); };
        }
        private string AppExePath()
        {
            return FirstExePathIn(AssemblyDirectory());
        }

        private string FirstExePathIn(string dir)
        {
            return Directory.GetFiles(dir, "*.exe").First();
        }

        private string AssemblyDirectory()
        {
            return @"\\vfs0006\RadData\oncology\ESimiele\fixCT\fixCT\bin\Debug";
        }
    }

}
