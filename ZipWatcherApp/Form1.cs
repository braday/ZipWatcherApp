using log4net;
using System;
using System.Collections.Concurrent;
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
        private bool _isActive { get; set; }

        private BlockingCollection<string> _blockingCollection;

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Form1()
        {
            InitializeComponent();
            _sevenZip = new SevenZip();
            _timer = new System.Timers.Timer();
            _fsw = new FileSystemWatcher();
            _blockingCollection = new BlockingCollection<string>();
        }

        private void showToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.Show();
        }

        private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
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

        private void btnOutput_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbdOutput = new FolderBrowserDialog();

            fbdOutput.Description = "Choose zip file output location";

            DialogResult result = fbdOutput.ShowDialog();

            if (result == DialogResult.OK && !string.IsNullOrEmpty(fbdOutput.SelectedPath))
            {
                /* e.g.
                    var directoryName = Path.GetFileName(txtInput.Text);
                    txtOutput.Text = Path.Combine(fbd.SelectedPath, directoryName + ".7z");
                */


                textBoxOutput.Text = fbdOutput.SelectedPath;

            }
        }

        // Start button
        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                FileSystemWatcher rootWatcher = _fsw;
                rootWatcher.Path = textBoxInput.Text;
                rootWatcher.InternalBufferSize = 65536; // 64k memory

                // TODO: make a err msg on label?
                if (string.IsNullOrWhiteSpace(textBoxInput.Text) & string.IsNullOrWhiteSpace(textBoxOutput.Text))

                {
                    const string msg = "You must choose a directory to watch.";
                    const string caption = "Warning";
                    //MessageBox.Show(msg, caption, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    lblResult.Text = msg;
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
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
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
                    /* TODO: notification of each folder creation with number shown */
                    string[] subDir = Directory.GetDirectories(textBoxInput.Text);
                    //int dirCount = Directory.GetDirectories(textBoxInput.Text).Length;

                    int i = 1;

                    while (i <= subDir.Length)
                    {
                        notifyIcon1.ShowBalloonTip(1000, "The number of file created", $"{i}", ToolTipIcon.Info); // it should be count 1, 2 ,3 and so on.
                        i++;
                    }

                    log.Info($"{e.Name} Directory {i} : {e.ChangeType} on {DateTime.Now.ToString()} \r\n");
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

                    // TODO 1: Timer input manually
                    // var aTimer = new System.Timers.Timer();
                    var aTimer = _timer;

                    int inputTimer = Convert.ToInt32(numUpDown.Value);

                    int iMinute = 1000 * 60;
                    aTimer.Interval = inputTimer * iMinute;

                    /* Lambda == args => expression
                       send event to subFolderWatcher
                    */
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

            /* TODO 2: distinct the file name with date time

            //string filePath = subFolderWatcher.Path.Substring(0, subFolderWatcher.Path.LastIndexOf(@"\") + 1);
            //string folderPath = subFolderWatcher.Path.Substring(0, subFolderWatcher.Path.LastIndexOf(@"\")); */

            /*
            string Source = textBoxInput.Text + "\\*";
            string Target = tBoxOutput.Text + "\\MyZip";
            _sevenZip.CreateZipFile(Source, Target);
            */

            string source = textBoxInput.Text + "\\";
            string target = tBoxOutput.Text + "\\everySingleZipFile";  // the target location only contains zip file from the source location
            foreach (var subfolder in Directory.GetFiles(source))
            {
                _sevenZip.CreateZipFile(source, target);
            }

            //_sevenZip.CreateZipFile(source, target);

            MessageBox.Show($@"zip file created at {DateTime.Now.ToString()}");

            subFolderWatcher.Dispose();
            aTimer.Dispose();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            _isActive = false;

            var stopWatcher = new FileSystemWatcher();
            stopWatcher.Path = textBoxInput.Text;
            stopWatcher.Created -= new FileSystemEventHandler(rootWatcher_Created);
            stopWatcher.EnableRaisingEvents = false;

            _timer.Stop();

            lblResult.Text = "Program has stopped, press start button to watch again.";

            // create a method to reset to the original state???
        }

        private void btnLog_Click(object sender, EventArgs e)
        {
            // TODO Open a log from this button, readonly
        }

        private void MinimiseToSystemTray(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)

            {
                this.Hide();

                notifyIcon1.ShowBalloonTip(1000, "Trim Zipper", "click here to resize", ToolTipIcon.Info);
            }
        }

        private void numSet_ValueChanged(object sender, EventArgs e)
        {
        }
    }
}
