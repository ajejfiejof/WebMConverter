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
        private string updateDownloadUrl;
        private string updateZipPath;
        private string updateTempPath;
        private string updateExePath;
        private string updateCertPath;

        public UpdateUI()
        {
            updateZipPath = Path.Combine(Path.GetTempPath(), "WebM-Converter-update.zip");
            updateTempPath = Path.Combine(Path.GetTempPath(), "WebM-Converter-update");
            updateExePath = Path.Combine(updateTempPath, "WebMConverter.exe");
            updateCertPath = Path.Combine(updateTempPath, "trusted.cer");

            InitializeComponent();
        }

        private void UpdateUI_Load(object sender, EventArgs e) => Step1_GetLatestVersion();

        private void Step1_GetLatestVersion()
        {
            labelStatus.Text = "Checking latest version...";

            Task.Factory.StartNew(() =>
            {
                try
                {
                    var release = Program.FetchLatestRelease();
                    latestVersion = release.Version;
                    updateDownloadUrl = release.DownloadUrl;

                    if (string.IsNullOrEmpty(updateDownloadUrl))
                    {
                        Invoke((Action)(() => Abort("No download URL found in release.")));
                        return;
                    }

                    Invoke((Action)(() => Step2_DownloadLatestVersion()));
                }
                catch (Exception ex)
                {
                    Invoke((Action)(() => Abort(ex.Message)));
                }
            });
        }

        private void Step2_DownloadLatestVersion()
        {
            labelStatus.Text = $"Downloading version {latestVersion}...";
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = 0;

            var client = new WebClient();
            client.Headers.Add("User-Agent", Program.UserAgent);
            client.DownloadProgressChanged += delegate(object sender, DownloadProgressChangedEventArgs args)
            {
                progressBar.Value = args.ProgressPercentage;
                labelStatus.Text = $"Downloading version {latestVersion} ({args.BytesReceived / 1024 / 1024} MiB / {args.TotalBytesToReceive / 1024 / 1024} MiB)";
            };

            client.DownloadFileCompleted += delegate(object sender, AsyncCompletedEventArgs args)
            {
                if (args.Error != null)
                {
                    Abort(args.Error.Message);
                    return;
                }

                Step3_UnzipAndVerify();
            };

            client.DownloadFileAsync(new Uri(updateDownloadUrl), updateZipPath);
        }

        private async void Step3_UnzipAndVerify()
        {
            labelStatus.Text = "Extracting files...";
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.Value = 30;

            if (Directory.Exists(updateTempPath))
                Directory.Delete(updateTempPath, true);

            Directory.CreateDirectory(updateTempPath);
            await Task.Run(() => ZipFile.ExtractToDirectory(updateZipPath, updateTempPath));
            File.Delete(updateZipPath);

            if (File.Exists("trusted.cer"))
            {
                try
                {
                    var trustedCertificate = new X509Certificate2("trusted.cer");
                    var exeCertificate = X509Certificate.CreateFromSignedFile(updateExePath);
                    if (!exeCertificate.Equals(trustedCertificate))
                    {
                        Abort("The downloaded executable is not signed by a trusted developer.");
                        return;
                    }
                }
                catch (Exception e)
                {
                    Abort("Certificate verification error: " + e.Message);
                    return;
                }
            }

            Step4_OverwriteAndRestart();
        }

        private void Step4_OverwriteAndRestart()
        {
            try
            {
                var source = new DirectoryInfo(updateTempPath);
                var target = new DirectoryInfo(Environment.CurrentDirectory);

                CopyAll(source, target);
            }
            catch (Exception e)
            {
                Abort(e.Message);
                return;
            }

            try
            {
                Directory.Delete(updateTempPath, true);
            }
            catch { }

            Process.Start("WebMConverter.exe");
            Application.Exit();
        }

        private void Abort(string message = null)
        {
            if (File.Exists(updateZipPath))
                File.Delete(updateZipPath);

            if (Directory.Exists(updateTempPath))
                Directory.Delete(updateTempPath, true);

            if (!string.IsNullOrEmpty(message))
                MessageBox.Show(message, "Update failed", MessageBoxButtons.OK, MessageBoxIcon.Error);

            Application.Exit();
        }

        private static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            if (Directory.Exists(target.FullName) == false)
            {
                Directory.CreateDirectory(target.FullName);
            }

            foreach (FileInfo fi in source.GetFiles())
            {
                fi.CopyTo(Path.Combine(target.ToString(), fi.Name), true);
            }

            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }
    }
}
