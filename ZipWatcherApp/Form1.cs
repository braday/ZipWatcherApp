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
        private StreamWriter _sw;


        public Form1()
        {
            InitializeComponent();
            this._sevenZip = new SevenZip();
            this._timer = new System.Timers.Timer();
            this._fsw = new FileSystemWatcher();
            this._sw = new StreamWriter(@"C:\Users\Patty\Downloads\testLog.txt");


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

            if (watchedPath.Contains("New folder")) { return; }

            var folder = Directory.Exists(watchedPath);
            var file = File.Exists(watchedPath);


            try
            {
                if (folder)
                {
                    _sw.WriteLine($"{e.Name} Directory : {e.ChangeType} on {DateTime.Now.ToString()} \r\n");

                }

                if (file)
                {
                    _sw.WriteLine($"{e.Name} File : {e.ChangeType} on {DateTime.Now.ToString()} \r\n");
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
            _sw.WriteLine($"restart the timer as {evt.Name} created on {DateTime.Now.ToString()}");
        }

        private void OnTimedEvent(object timerSender, ElapsedEventArgs timerEvt, FileSystemWatcher subFolderWatcher)
        {
            subFolderWatcher.EnableRaisingEvents = false;

            // Explicit Casting
            var aTimer = (System.Timers.Timer)timerSender;
            //aTimer.Stop();

            _sw.WriteLine($"File zipped at {timerEvt.SignalTime} \r\n");

            _sw.Close();

            //string subPath = subFolderWatcher.Path.Substring(0, subFolderWatcher.Path.LastIndexOf(@"\") + 1);
            //string archive = subFolderWatcher.Path.Substring(0, subFolderWatcher.Path.LastIndexOf(@"\"));
            _sevenZip.CreateZipFolder(subFolderWatcher.Path, subFolderWatcher.Path + ".7z");
            subFolderWatcher.Dispose();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            _isActive = false;

            // TODO new a watcher and access to the original watcher???
            var stopWatcher = new FileSystemWatcher();
            stopWatcher.Path = textBox.Text;
            stopWatcher.Created -= new FileSystemEventHandler(rootWatcher_Created);
            stopWatcher.EnableRaisingEvents = false;

            _timer.Stop();

            stopWatcher.Dispose();
            _timer.Dispose();

            lblResult.Text = "Program has stopped, press start button to watch again.";

            // create a method to reset to the original state?
        }

        private void btnLog_Click(object sender, EventArgs e)
        {
            // TODO create a log file class call LogList?
        }

        private void lblResult_Click(object sender, EventArgs e)
        {
        }
    }
}