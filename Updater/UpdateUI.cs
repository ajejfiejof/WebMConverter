using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WebMConverter.Updater
{
    public partial class UpdateUI : Form
    {
        private string latestVersion;
        private string updateZipPath;
        private string updateTempPath;
        private string updateExePath;

        public UpdateUI()
        {
            updateZipPath = Path.Combine(Path.GetTempPath(), "WebM-for-Retards-update.zip");
            updateTempPath = Path.Combine(Path.GetTempPath(), "WebM-for-Retards-update");
            updateExePath = Path.Combine(updateTempPath, "WebMConverter.exe");

            InitializeComponent();
        }

        private void UpdateUI_Load(object sender, EventArgs e)
        {
            Step1_GetLatestVersion();
        }

        private void Step1_GetLatestVersion()
        {
            labelStatus.Text = "Checking latest version...";

            var client = new WebClient();
            client.DownloadStringCompleted += delegate(object sender, DownloadStringCompletedEventArgs args)
            {
                if (args.Error != null)
                {
                    Abort(args.Error.Message);
                    Application.Exit();
                }

                latestVersion = args.Result.Trim();
                Step2_DownloadLatestVersion();
            };

            client.DownloadStringAsync(new Uri(Program.VersionUrl));
        }

        private void Step2_DownloadLatestVersion()
        {
            labelStatus.Text = string.Format("Downloading version {0} (....)", latestVersion);
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = 0;

            var client = new WebClient();
            client.DownloadProgressChanged += delegate(object sender, DownloadProgressChangedEventArgs args)
            {
                progressBar.Value = args.ProgressPercentage;

                labelStatus.Text = string.Format("Downloading version {0} ({1} MiB / {2} MiB)", latestVersion, args.BytesReceived / 1024 / 1024, args.TotalBytesToReceive / 1024 / 1024);
            };

            client.DownloadFileCompleted += delegate(object sender, AsyncCompletedEventArgs args)
            {
                if (args.Error != null)
                {
                    Abort(args.Error.Message);
                    Application.Exit();
                }

                Step3_UnzipAndVerify();
            };

            client.DownloadFileAsync(new Uri(string.Format(Program.ProgramUrl, latestVersion)), updateZipPath);
        }

        private async void Step3_UnzipAndVerify()
        {
            labelStatus.Text = string.Format("Verifying integrity...");
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.Value = 30;

            if (Directory.Exists(updateTempPath))
                Directory.Delete(updateTempPath, true);

            Directory.CreateDirectory(updateTempPath);
            await Task.Run(() => ZipFile.ExtractToDirectory(updateZipPath, updateTempPath));
            File.Delete(updateZipPath);

            X509Certificate2 nixxCertificate = new X509Certificate2("trusted.cer");
            X509Certificate exeCertificate;
#if !DEBUG
            try
            {
                exeCertificate = X509Certificate.CreateFromSignedFile(updateExePath);
            }
            catch (Exception)
            {
                Abort("The downloaded executable is unsigned and therefore untrusted.");
                return;
            }

            if (!exeCertificate.Equals(nixxCertificate))
            {
                Abort("The downloaded executable is not signed by a trusted developer.");
                return;
            }
#endif

            Step4_OverwriteAndRestart();
        }

        private void Step4_OverwriteAndRestart()
        {
            try
            {
                var entries = Directory.EnumerateFileSystemEntries(updateTempPath);

                foreach (var entry in entries)
                {
                    var name = Path.GetFileName(entry);
                    var entryAttributes = File.GetAttributes(entry);

                    Console.WriteLine(entry);
                    Console.WriteLine(name);

                    if (Directory.Exists(name))
                        Directory.Delete(name, true);
                    if (File.Exists(name))
                        File.Delete(name);

                    Directory.Move(entry, name);
                }
            }
            catch (Exception e)
            {
                Abort(e.Message);
            }

            Directory.Delete(updateTempPath);
            Process.Start("WebMConverter.exe");
            Application.Exit();
        }

        private void Abort(string message)
        {
            if (File.Exists(updateZipPath))
                File.Delete(updateZipPath);

            if (Directory.Exists(updateTempPath))
                Directory.Delete(updateTempPath, true);

            MessageBox.Show(message, "Update failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Application.Exit();
        }
    }
}
