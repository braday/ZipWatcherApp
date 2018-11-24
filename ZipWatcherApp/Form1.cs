using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;


namespace ZipWatcherApp
{
    public class SevenZip
    {
        public void CreateZipFolder(string sourceName, string targetName)
        {
            ProcessStartInfo zipProcess = new ProcessStartInfo();
            zipProcess.FileName = @"E:\Program Files\7-Zip\7z.exe"; // select the 7zip program to start
            zipProcess.Arguments = "a -t7z \"" + targetName + "\" \"" + sourceName + "\" -mx=9";
            zipProcess.WindowStyle = ProcessWindowStyle.Minimized;
            Process zip = Process.Start(zipProcess);
            zip.WaitForExit();
        }
    }

    public partial class Form1 : Form
    {
        private Boolean isActive;

        public Form1()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            OpenFileDialog ofd = new OpenFileDialog();

            fbd.Description = $"Choose a Folder to Watch";

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                // show the path in the text box
                this.textBox.Text = fbd.SelectedPath;
            }
        }

        private void textBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            isActive = true;

            FileSystemWatcher rootWatcher = new FileSystemWatcher();
            rootWatcher.Path = textBox.Text;
            rootWatcher.Created += new FileSystemEventHandler(rootWatcher_Created);
            rootWatcher.EnableRaisingEvents = true;
            rootWatcher.Filter = "*.*";
        }

        private void rootWatcher_Created(object sender, FileSystemEventArgs e)
        {
            string watchedPath = e.FullPath; // watch the root folder for any file creation.

            var isFolder = Directory.Exists(watchedPath);
            var isFile = File.Exists(watchedPath);

            //MessageBox.Show(watchRootLv);

            while (watchedPath.Contains("New folder"))
            {
                MessageBox.Show($"naming not accepted.");
                return;
            }
            MessageBox.Show($"{e.Name} Directory : {e.ChangeType} on {DateTime.Now.ToString()} \r\n");


            if (isFile)
            {
                MessageBox.Show($"{e.Name} File : {e.ChangeType} on {DateTime.Now.ToString()} \r\n");
                return;
            }

            // create another watcher for file creation and send event to timer
            FileSystemWatcher subFolderWatcher = new FileSystemWatcher();
            subFolderWatcher.Path = watchedPath;

            if (isActive)
            {
                var aTimer = new System.Timers.Timer();
                aTimer.Interval = 15000;

                // Lambda == args => expression
                // send event to subFolderWatcher
                aTimer.Elapsed += new ElapsedEventHandler((timerSender, timerEvt) =>
                    OnTimedEvent(timerSender, timerEvt, subFolderWatcher));
                aTimer.AutoReset = false;
                aTimer.Enabled = true;

                // sub-folder sends event to timer (and wait timer to notify subfolder)
                subFolderWatcher.Created +=
                    new FileSystemEventHandler((s, evt) => subFolderWatcher_Created(s, evt, aTimer));
                subFolderWatcher.Filter = "*.*";
                subFolderWatcher.EnableRaisingEvents = true;
            }

        }

        private void subFolderWatcher_Created(object sender, FileSystemEventArgs evt, System.Timers.Timer aTimer)
        {
            // if new file created, stop the timer
            //  then restart the timer
            aTimer.AutoReset = false;
            aTimer.Stop();
            aTimer.Enabled = false;
            aTimer.Enabled = true;
            Console.WriteLine($"restart the timer as {evt.Name} created on {DateTime.Now.ToString()}");

        }

        private void OnTimedEvent(object timerSender, ElapsedEventArgs timerEvt, FileSystemWatcher subFolderWatcher)
        {
            subFolderWatcher.EnableRaisingEvents = false;

            // Explicit Casting
            Timer timer = timerSender as Timer;
            if (timer != null) timer.Stop();
            //timer.Dispose();

            // Once time elapsed, zip the folder here?
            Console.WriteLine($"time up. zip process begin at {timerEvt.SignalTime} \r\n");

            var file = new FileInfo(textBox.Text);
            var parentDir = file.Directory == null ? null : file.Directory.Parent; // test if dir or not

            if (parentDir != null)
            {
                SevenZip zip = new SevenZip();
                zip.CreateZipFolder(subFolderWatcher.Path, subFolderWatcher.Path + ".7z");
            }

            subFolderWatcher.Dispose();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            isActive = false;
        }
    }
}
