using System;
using System.ComponentModel;
using System.IO;
using System.Text;
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
        private BackgroundWorker _backgroundWorker;

        public Form1()
        {
            InitializeComponent();
            this._sevenZip = new SevenZip();
            this._fsw = new FileSystemWatcher();
            this._backgroundWorker = new BackgroundWorker();


            _backgroundWorker.WorkerReportsProgress = true;
            _backgroundWorker.WorkerSupportsCancellation = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _isActive = false;

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
            _backgroundWorker.RunWorkerAsync();

            FileSystemWatcher rootWatcher = _fsw;
            rootWatcher.Path = textBox.Text;
            rootWatcher.Created += new FileSystemEventHandler(rootWatcher_Created);
            rootWatcher.EnableRaisingEvents = true;
            rootWatcher.Filter = "*.*";

            lblResult.Text = string.Format($"Start Watching...");

            _isActive = true;
        }

        private void rootWatcher_Created(object sender, FileSystemEventArgs e)
        {
            string watchedPath = e.FullPath; // watch the root folder for any file creation.

            var folder = Directory.Exists(watchedPath);
            var file = File.Exists(watchedPath);

            if (folder)
            {
                if (watchedPath.Contains("New folder"))
                {
                    MessageBox.Show($"naming not accepted.").ToString();
                    return;
                }
                MessageBox.Show($"{e.Name} Directory : {e.ChangeType} on {DateTime.Now.ToString()} \r\n");
            }
            if (file)
            {
                MessageBox.Show($"{e.Name} File : {e.ChangeType} on {DateTime.Now.ToString()} \r\n");
                return;
            }

            if (_isActive)
            {
                // create another watcher for file creation and send event to timer
                FileSystemWatcher subFolderWatcher = new FileSystemWatcher();
                subFolderWatcher.Path = watchedPath;

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
            aTimer.Enabled = false;
            aTimer.Enabled = true;
            MessageBox.Show($"restart the timer as {evt.Name} created on {DateTime.Now.ToString()}");
        }

        private void OnTimedEvent(object timerSender, ElapsedEventArgs timerEvt, FileSystemWatcher subFolderWatcher)
        {
            subFolderWatcher.EnableRaisingEvents = false;

            // Explicit Casting
            if (timerSender is System.Timers.Timer timer) timer.Stop();

            //timer.Dispose();

            // Once time elapsed, zip the folder here?
            Console.WriteLine($"time up. zip process begin at {timerEvt.SignalTime} \r\n");

            var file = new FileInfo(textBox.Text);
            var parentDir = file.Directory == null ? null : file.Directory.Parent; // test if dir or not

            if (parentDir != null)
            {
                _sevenZip.CreateZipFolder(subFolderWatcher.Path, subFolderWatcher.Path + ".7z");
            }

            subFolderWatcher.Dispose();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            _isActive = false;

            // new a watcher and access to the original watcher...
            var stopWatcher = new FileSystemWatcher();



            lblResult.Text = "Press Start button to watch.";

            // create a method to reset to the original state?
            _backgroundWorker.CancelAsync();
        }

        private void btnLog_Click(object sender, EventArgs e)
        {
            // create a log file class call LogList?
            StringBuilder sb = new StringBuilder();
        }

        private void lblResult_Click(object sender, EventArgs e)
        {
        }

    }
}