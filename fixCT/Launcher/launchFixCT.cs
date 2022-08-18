using System;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using VMS.TPS.Common.Model.API;
using System.Windows;


namespace VMS.TPS
{
    public class Script
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Execute(ScriptContext context)
        {
            try
            {
                Process.Start(AppExePath());
            }
            catch (Exception e) { MessageBox.Show(String.Format("{0}\n{1}",AppExePath(), e.Message)); };
        }
        private string AppExePath()
        {
            return FirstExePathIn(Path.GetDirectoryName(GetSourceFilePath()));
        }

        private string FirstExePathIn(string dir)
        {
            return Directory.GetFiles(dir, "*.exe").First();
        }

        private string GetSourceFilePath([CallerFilePath] string sourceFilePath = "")
        {
            return sourceFilePath;
        }
    }
}
