using System.Diagnostics;
using System.IO;

namespace ZipWatcherApp
{
    public class SevenZip
    {
        public void CreateZipFile(string sourceName, string targetName)
        {
            ProcessStartInfo zipProcess = new ProcessStartInfo();
            zipProcess.FileName = @"E:\Program Files\7-Zip\7z.exe"; // select the 7zip program to start
            zipProcess.Arguments = "a -t7z \"" + targetName + "\" \"" + sourceName + "\" -mx=9";
            zipProcess.WindowStyle = ProcessWindowStyle.Minimized;
            Process zip = Process.Start(zipProcess);
            zip.WaitForExit();
        }
    }
}