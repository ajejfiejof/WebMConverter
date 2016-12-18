using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace WebMConverter.Dialogs
{
    public partial class DownloadDialog : Form
    {
        public string Outfile;

        private readonly string _infile;
        private YoutubeDL _filenameGetterProcess;
        private YoutubeDL _downloaderProcess;

        private Timer _timer;
        private bool _ended;
        private bool _panic;
        private bool _gettingFilename;

        public DownloadDialog(string url)
        {
            InitializeComponent();
            pictureStatus.BackgroundImage = StatusImages.Images["Happening"];

            _infile = '"' + url.Replace(@"""", @"\""") + '"';
            Outfile = Path.GetTempPath();
            Trace.TraceInformation(_infile);
        }

        private void ProcessOnErrorDataReceived(object sender, DataReceivedEventArgs args)
        {
            if (args.Data != null)
                boxOutput.Invoke((Action)(() => boxOutput.AppendText('\n' + args.Data)));
        }

        private void FilenameGetterOnOutputDataReceived(object sender, DataReceivedEventArgs args)
        {
            if (args.Data != null)
                Outfile = Path.Combine(Outfile, args.Data);
        }

        private void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs args)
        {
            if (args.Data != null)
                boxOutput.Invoke((Action)(() => boxOutput.AppendText('\n' + args.Data)));
        }

        private void DownloadDialog_Load(object sender, EventArgs e)
        {
            _filenameGetterProcess = new YoutubeDL("--get-filename " + _infile);
            _downloaderProcess = new YoutubeDL(null);

            _gettingFilename = true;

            _filenameGetterProcess.ErrorDataReceived += ProcessOnErrorDataReceived;
            _filenameGetterProcess.OutputDataReceived += FilenameGetterOnOutputDataReceived;
            _filenameGetterProcess.Exited += (o, args) => boxOutput.Invoke((Action) (() =>
            {
                if (_panic) return;
                boxOutput.AppendText(string.Format("\nDownloading to {0}", Outfile));
                _downloaderProcess.StartInfo.Arguments = @"-o """ + Outfile + @""" " + _infile;

                _timer = new Timer { Interval = 500 };
                _timer.Tick += Exited;
                _timer.Start();
            }));

            _downloaderProcess.ErrorDataReceived += ProcessOnErrorDataReceived;
            _downloaderProcess.OutputDataReceived += ProcessOnOutputDataReceived;
            _downloaderProcess.Exited += (o, args) => boxOutput.Invoke((Action)(() =>
            {
                if (_panic) return; //This should stop that one exception when closing the converter
                boxOutput.AppendText("\n--- YOUTUBE-DL HAS EXITED ---");
                buttonCancel.Enabled = false;

                _timer = new Timer {Interval = 500};
                _timer.Tick += Exited;
                _timer.Start();
            }));

            _filenameGetterProcess.Start();
        }

        private void Exited(object sender, EventArgs eventArgs)
        {
            _timer.Stop();

            if (_gettingFilename)
            {
                if (_filenameGetterProcess.ExitCode != 0)
                {
                    boxOutput.AppendText(string.Format("\n\nyoutube-dl.exe exited with exit code {0}. That's usually bad.", _filenameGetterProcess.ExitCode));
                    boxOutput.AppendText("\nIf you have no idea what went wrong, open an issue on GitGud and copy paste the output of this window there.");
                    pictureStatus.BackgroundImage = StatusImages.Images["Failure"];
                    buttonCancel.Enabled = true;
                    _ended = true;
                }
                else
                {
                    _gettingFilename = false;
                    _downloaderProcess.Start();
                }
                return;
            }

            if (_downloaderProcess.ExitCode != 0)
            {
                boxOutput.AppendText(string.Format("\n\nyoutube-dl.exe exited with exit code {0}. That's usually bad.", _downloaderProcess.ExitCode));
                boxOutput.AppendText("\nIf you have no idea what went wrong, open an issue on GitGud and copy paste the output of this window there.");
                pictureStatus.BackgroundImage = StatusImages.Images["Failure"];
                buttonCancel.Enabled = true;
            }
            else
            {
                boxOutput.AppendText("\n\nVideo downloaded succesfully!");
                pictureStatus.BackgroundImage = StatusImages.Images["Success"];
                buttonLoad.Enabled = true;

                // workaround for https://github.com/rg3/youtube-dl/issues/11472
                if (!File.Exists(Outfile))
                {
                    Outfile = Path.ChangeExtension(Outfile, "mkv");
                }
            }

            _ended = true;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            if (!_ended || _panic) //Prevent stack overflow
            {
                if (!_filenameGetterProcess.HasExited)
                    Program.KillProcessAndChildren(_filenameGetterProcess.Id);

                if (_gettingFilename)
                    return;

                if (!_downloaderProcess.HasExited)
                    Program.KillProcessAndChildren(_downloaderProcess.Id);
            }
            else
                Close();
        }

        private void ConverterForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _panic = true; //Shut down while avoiding exceptions
            buttonCancel_Click(sender, e);
        }

        private void ConverterForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _filenameGetterProcess.Dispose();
            _downloaderProcess.Dispose();
        }

        private void buttonLoad_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
