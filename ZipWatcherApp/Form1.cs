using log4net;
using System;
using System.IO;
using System.Timers;
using System.Windows.Forms;

namespace ZipWatcherApp
{
    public partial class Form1 : Form
    {
        private FileSystemWatcher _fsw;
        private System.Timers.Timer _timer;
        private SevenZip _sevenZip;
        private bool _isActive;

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Form1()
        {
            InitializeComponent();
            this._sevenZip = new SevenZip();
            this._timer = new System.Timers.Timer();
            this._fsw = new FileSystemWatcher();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _isActive = false;
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();

            fbd.Description = $"Choose a input directory";

            if (fbd.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                // show the path in the text box
                this.textBoxInput.Text = fbd.SelectedPath;
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            FileSystemWatcher rootWatcher = _fsw;
            rootWatcher.Path = textBoxInput.Text;

            if (string.IsNullOrWhiteSpace(textBoxInput.Text))
            {
                const string msg = "You must choose a directory to watch.";
                const string caption = "Warning";
                MessageBox.Show(msg, caption, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (rootWatcher.EnableRaisingEvents) return;

            rootWatcher.EnableRaisingEvents = true;
            rootWatcher.Filter = "*.*";
            rootWatcher.Created += rootWatcher_Created;
            rootWatcher.IncludeSubdirectories = true;


            lblResult.Text = string.Format($"Start Process...");

            _isActive = true;
        }

        private void rootWatcher_Created(object sender, FileSystemEventArgs e)
        {
            string watchedPath = e.FullPath; // watch the root folder for any file creation.

            if (watchedPath.Contains("New folder")) { return; }

            var folder = Directory.Exists(watchedPath);
            var file = File.Exists(watchedPath);

            try
            {
                if (folder)
                {
                    // TODO: notification of folder created plus number count
                    var dirInfo = new DirectoryInfo(watchedPath);
                    int dirCount = dirInfo.GetDirectories().Length;

                    log.Info($"{e.Name} Directory : {e.ChangeType} on {DateTime.Now.ToString()} \r\n");
                }
                else
                {
                    log.Info($"{e.Name} File : {e.ChangeType} on {DateTime.Now.ToString()} \r\n");
                    return;
                }

                if (_isActive)
                {
                    // create another watcher for file creation and send event to timer
                    FileSystemWatcher subFolderWatcher = new FileSystemWatcher();

                    // TODO fix system arugment exception?
                    subFolderWatcher.Path = watchedPath;

                    //var aTimer = new System.Timers.Timer();
                    var aTimer = _timer;
                    aTimer.Interval = 15000;

                    // Lambda == args => expression
                    // send event to subFolderWatcher
                    aTimer.Elapsed += new ElapsedEventHandler((timerSender, timerEvt) => OnTimedEvent(timerSender, timerEvt, subFolderWatcher));
                    aTimer.AutoReset = false;
                    aTimer.Enabled = true;
                    aTimer.AutoReset = false;

                    // sub-folder sends event to timer (and wait timer to notify subfolder)
                    subFolderWatcher.Created +=
                        new FileSystemEventHandler((s, evt) => subFolderWatcher_Created(s, evt, aTimer));
                    //subFolderWatcher.Filter = "*.*";
                    subFolderWatcher.EnableRaisingEvents = true;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }

        private void subFolderWatcher_Created(object sender, FileSystemEventArgs evt, System.Timers.Timer aTimer)
        {
            // if new file created, stop the timer
            //  then restart the timer
            aTimer.AutoReset = false;
            aTimer.Enabled = false;
            aTimer.Enabled = true;
            log.Info($"restart the timer as {evt.Name} created on {DateTime.Now.ToString()}");
        }

        private void OnTimedEvent(object timerSender, ElapsedEventArgs timerEvt, FileSystemWatcher subFolderWatcher)
        {
            subFolderWatcher.EnableRaisingEvents = false;

            // Explicit Casting
            var aTimer = (System.Timers.Timer)timerSender;
            aTimer.Stop();


            //string filePath = subFolderWatcher.Path.Substring(0, subFolderWatcher.Path.LastIndexOf(@"\") + 1);
            //string folderPath = subFolderWatcher.Path.Substring(0, subFolderWatcher.Path.LastIndexOf(@"\"));
            // TODO: select different path for output

            //List<string> files = Directory.GetFiles(parentpath).ToList();

            //foreach (string file in files)
            //{
            //    if (file.Contains(path + ".jrn"))
            //    {
            //        string newtarget = Path.GetFileName(file);
            //        //copy journal file to folder
            //        File.Copy(file, SubWatcher.Path + @"\" + newtarget);
            //    }
            //}

            string inputDir = subFolderWatcher.Path;
            string outputDir = string.Format(tBoxOutput.Text);
            _sevenZip.CreateZipFile(subFolderWatcher.Path, subFolderWatcher.Path + ".7z");

            //log.Info($@"zip file: {subFolderWatcher.Path}.7z created at {DateTime.Now.ToString()}");
            // TODO: notification at the toolbar

            MessageBox.Show($@"zip file: {Path.GetFileName(subFolderWatcher.Path)}.7z created at {DateTime.Now.ToString()}");

            subFolderWatcher.Dispose();
            aTimer.Dispose();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            _isActive = false;

            //var stopWatcher = new FileSystemWatcher();
            //stopWatcher.Path = textBox.Text;
            //stopWatcher.Created -= new FileSystemEventHandler(rootWatcher_Created);
            //stopWatcher.EnableRaisingEvents = false;

            _timer.Stop();

            lblResult.Text = "Program has stopped, press start button to watch again.";

            // create a method to reset to the original state?
        }

        private void btnLog_Click(object sender, EventArgs e)
        {
            // TODO Open a log from this button, readonly
        }

        private void btnOutput_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();

            sfd.Filter = "zip files (*.7z) | *.7z";

            DialogResult result = sfd.ShowDialog();

            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(sfd.FileName))
            {
                tBoxOutput.Text = sfd.FileName;
            }

        }
    }
}