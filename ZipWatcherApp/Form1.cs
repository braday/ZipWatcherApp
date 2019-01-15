using log4net;
using System;
using System.IO;
using System.Timers;
using System.Windows.Forms;

namespace ZipWatcherApp
{
    public partial class Form1 : Form
    {
        private readonly FileSystemWatcher _fsw;
        private readonly System.Timers.Timer _timer;
        private readonly SevenZip _sevenZip;
        private bool _isActive { get; set; }
        private int _timeLeft = 0;
        //private Process process;

        //private BlockingCollection<string> _blockingCollection;

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Form1()
        {
            InitializeComponent();
            this._sevenZip = new SevenZip();
            this._fsw = new FileSystemWatcher();
            this._timer = new System.Timers.Timer();
            //_blockingCollection = new BlockingCollection<string>();
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
            FolderBrowserDialog fbd = new FolderBrowserDialog
            {
                Description = $@"Choose a input directory"
            };

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
                txtBoxOutput.Text = fbdOutput.SelectedPath;
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
                if (string.IsNullOrWhiteSpace(textBoxInput.Text) & string.IsNullOrWhiteSpace(txtBoxOutput.Text))

                {
                    const string msg = "You must choose a directory to watch.";
                    const string caption = "Warning";
                    MessageBox.Show(msg, caption, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    //lblResult.Text = msg;
                    return;
                }

                //if (rootWatcher.EnableRaisingEvents) return;

                rootWatcher.EnableRaisingEvents = true;
                rootWatcher.Filter = "*.*";
                rootWatcher.Created += rootWatcher_Created;
                rootWatcher.IncludeSubdirectories = true;

                // TODO 3. Show timer once detect folder creation
                _timeLeft = (int)numUpDown.Value;
                //timeLabel.Text = $"{timeLeft} seconds";
                timerCounter.Start();

                lblResult.Text = $@"Process Started";

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

                    var aTimer = _timer; //_timer = new System.Timers.Timer();

                    int userInputTime = Convert.ToInt32(numUpDown.Value);
                    int iSecond = 1000; // 1 millisecond
                    aTimer.Interval = userInputTime * iSecond;

                    /*
                     *Lambda == args => expression, send event to subFolderWatcher
                    */
                    aTimer.Elapsed += new ElapsedEventHandler((timerSender, timerEvt) => OnTimedEvent(timerSender, timerEvt, subFolderWatcher));
                    aTimer.AutoReset = false;
                    aTimer.Enabled = true;

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
            aTimer.AutoReset = false;
            aTimer.Enabled = false;
            aTimer.Enabled = true;

            //TODO 5: show reset timer and restart timer
            log.Info($"restart the timer as {evt.Name} created on {DateTime.Now.ToString()}");
            lblResult.Text = $@"restart the timer as {evt.Name} created on {DateTime.Now.ToString()}";
        }

        private void OnTimedEvent(object timerSender, ElapsedEventArgs timerEvt, FileSystemWatcher subFolderWatcher)
        {
            subFolderWatcher.EnableRaisingEvents = false;

            // Explicit Casting
            var aTimer = (System.Timers.Timer)timerSender;

            aTimer.Stop();

            foreach (var subfolder in Directory.GetDirectories(textBoxInput.Text))
            {
                string source = Path.GetFileName(subfolder);
                string target = Path.Combine(txtBoxOutput.Text, string.Format("{0}_{1}", source, DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".7z"));
                _sevenZip.CreateZipFile(subfolder, target);
            }
            MessageBox.Show($@"zip file created at {DateTime.Now.ToString()}");

            subFolderWatcher.Dispose();
            aTimer.Dispose();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            //TODO 4: Stop and restart the timer
            // Set all text boxes empty
            // When stop button click, Start button disable

            _timer.Stop();
            timerCounter.Stop();
            numUpDown.Value = 0;
            timeLabel.Text = 0 + @" seconds";
            lblResult.Text = @"Program has stopped, press start button to watch again.";

            _fsw.EnableRaisingEvents = false;

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
                notifyIcon1.ShowBalloonTip(1000, "Trim Zipper", "resize me", ToolTipIcon.Info);
            }
        }

        private void timerCounter_Tick(object sender, EventArgs e)
        {
            //TODO: if new file created, restart the timer
            if (_timeLeft > 0)
            {
                _timeLeft = _timeLeft - 1;
                timeLabel.Text = _timeLeft + @" seconds";
                lblResult.Text = $@"Process began and counting down in {timeLabel.Text} ";
            }
            else
            {
                timerCounter.Stop();
                timerCounter.Enabled = false;
                timeLabel.Text = $@"Time's up";

            }
        }

        private void textBoxInput_TextChanged(object sender, EventArgs e)
        {
            btnStart.Enabled = !string.IsNullOrEmpty(textBoxInput.Text) ? true : false;
        }

    }
}