using System;
using System.Diagnostics;

namespace ZipWatcherApp
{
    public class SevenZip
    {
        public void CreateZipFile(string sourceName, string targetName)
        {
            try
            {
                ProcessStartInfo zipProcess = new ProcessStartInfo();
                zipProcess.FileName = @"C:\Program Files\7-Zip\7z.exe"; // select the 7zip program to start
                zipProcess.Arguments = "a -t7z \"" + targetName + "\" \"" + sourceName + "\" -mx=9";
                zipProcess.WindowStyle = ProcessWindowStyle.Minimized;
                Process zip = Process.Start(zipProcess);
                zip.WaitForExit();
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }
    }
}