using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Boxroom_Studio_Updater
{
    [XmlRoot("Update")]
    public class UpdateInfo
    {
        public string LatestVersion { get; set; } = "";

        public string DownloadUrlWindows { get; set; } = "";

        public string DownloadUrlLinux { get; set; } = "";

        public string ReleaseNotes { get; set; } = "";

        public bool RequiresManualInstallation { get; set; } = false;
    }

    public partial class MainWindow : Window
    {
        public Version CurrentVersion { get; set; } = new Version(0, 0, 0, 0);

        public UpdateInfo? LatestUpdateInfo { get; set; }
        public string DownloadedZip { get; private set; }

        private readonly int _studioPid;
        public MainWindow()
        {
            InitializeComponent();


            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
                int.TryParse(args[1], out _studioPid);
            GetCurrentVersion("version.txt");
            CheckUpdateVersion("https://boxroom-studio.hempton.us/update.xml");
        }

        /// <summary>
        /// Checks for updates by fetching the latest version information from a specified URL.
        /// </summary>
        /// <param name="v"></param>
        private void CheckUpdateVersion(string v)
        {
            // Fetch the latest version information from the specified URL
            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    string xmlContent = client.GetStringAsync(v).Result;
                    XmlSerializer serializer = new XmlSerializer(typeof(UpdateInfo));
                    using (var reader = new System.IO.StringReader(xmlContent))
                    {
                        UpdateInfo updateInfo = (UpdateInfo)serializer.Deserialize(reader);
                        LatestUpdateInfo = updateInfo;

                        Version latest = new Version(updateInfo.LatestVersion);

                        if (latest > CurrentVersion)
                        {
                            StatusText.Text = $"⬆️ Update available: {updateInfo.LatestVersion}";
                            ReleaseNotes.Text = updateInfo.ReleaseNotes;



                            UpdateButton.IsEnabled = true;

                        }
                        else
                        {
                            ReleaseNotes.Text = "You are using the latest version.";
                            // No update available
                            StatusText.Text = "No update available.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., network errors, XML parsing errors)
                StatusText.Text = $"Error checking for updates: {ex.Message}";
                Debug.WriteLine(ex);

            }
        }

        /// <summary>
        /// Reads the current version of the application from a specified file.
        /// </summary>
        /// <param name="v"></param>
        private void GetCurrentVersion(string v)
        {
            // Read the current version from the specified file
            if (System.IO.File.Exists(v))
            {
                CurrentVersion = new Version(System.IO.File.ReadAllText(v).Trim());
                VersionText.Text = $"Current Version: {CurrentVersion}";
            }
            else
            {
                CurrentVersion = new Version(0, 0, 0, 0);
                VersionText.Text = $"Current Version: {CurrentVersion}";
            }
        }

        private async void UpdateButton_Click(object? sender, RoutedEventArgs e)
        {
            UpdateButton.Content = "Downloading...";

            try
            {
                await DownloadUpdateAsync();

                if (LatestUpdateInfo?.RequiresManualInstallation == true)
                {
                    StatusText.Text =
                        "Manual installation required. The update has been downloaded.";

                    UpdateButton.IsEnabled = false;
                    CancelButton.Content = "Open Folder";

                    return;
                }

                await WaitForStudioToCloseAsync();
                await ExtractUpdateAsync();

                LaunchStudio();

                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                StatusText.Text = ex.Message;
                Debug.WriteLine(ex);

                UpdateButton.IsEnabled = true;
                CancelButton.IsEnabled = true;
            }
        }

        private void LaunchStudio()
        {
            string exeName = OperatingSystem.IsWindows()
                ? "Boxroom Studio.exe"
                : "Boxroom Studio";

            string exePath = Path.Combine(
                AppContext.BaseDirectory,
                exeName);

            if (!File.Exists(exePath))
            {
                throw new FileNotFoundException(
                    "Unable to locate Boxroom Studio.",
                    exePath);
            }

            StatusText.Text = "Launching Boxroom Studio...";

            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                WorkingDirectory = AppContext.BaseDirectory
            });
        }

        private async Task ExtractUpdateAsync()
        {
            if (string.IsNullOrWhiteSpace(DownloadedZip))
                throw new Exception("No downloaded update was found.");

            if (!File.Exists(DownloadedZip))
                throw new FileNotFoundException(
                    "Downloaded update archive not found.",
                    DownloadedZip);

            StatusText.Text = "Extracting update...";
            ProgressBar.IsIndeterminate = true;

            string installPath = AppContext.BaseDirectory;

            // Extract over the existing installation
            ZipFile.ExtractToDirectory(
                DownloadedZip,
                installPath,
                overwriteFiles: true);

            // Cleanup
            File.Delete(DownloadedZip);

            ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = 100;

            StatusText.Text = "Update installed.";
        }

        private async Task WaitForStudioToCloseAsync()
        {
            if (_studioPid <= 0)
                return;

            try
            {
                StatusText.Text = "Waiting for Boxroom Studio to close...";

                Process process = Process.GetProcessById(_studioPid);

                await process.WaitForExitAsync();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }

        /// <summary>
        /// Downloads the update asynchronously.
        /// </summary>
        /// <returns></returns>
        private async Task DownloadUpdateAsync()
        {
            string? downloadUrl = OperatingSystem.IsWindows()
                ? LatestUpdateInfo?.DownloadUrlWindows
                : LatestUpdateInfo?.DownloadUrlLinux;

            if (string.IsNullOrWhiteSpace(downloadUrl))
                throw new Exception("Download URL is not available for this platform.");

            DownloadedZip = Path.Combine(
                AppContext.BaseDirectory,
                "Boxroom-Studio-Update.zip");


            StatusText.Text = $"Downloading to: {DownloadedZip}";
            Debug.WriteLine($"Downloading to: {DownloadedZip}");
            // Remove any previous download
            if (File.Exists(DownloadedZip))
                File.Delete(DownloadedZip);

            StatusText.Text = "Downloading update...";
            ProgressBar.IsIndeterminate = true;

            using HttpClient client = new();

            using HttpResponseMessage response =
                await client.GetAsync(
                    downloadUrl,
                    HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            await using Stream input =
                await response.Content.ReadAsStreamAsync();

            await using FileStream output = new(
                DownloadedZip,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None);

            await input.CopyToAsync(output);

            ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = 100;

            StatusText.Text = "Download complete.";
        }

        private void CancelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (LatestUpdateInfo?.RequiresManualInstallation == true)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = AppContext.BaseDirectory,
                    UseShellExecute = true
                });

                Environment.Exit(0);
            }
            else
            {
                Environment.Exit(0);
            }
        }
    }
}