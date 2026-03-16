using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows.Controls;

namespace diskform4th.UI
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<string> AvailableDrives { get; set; } = new ObservableCollection<string>();

        // Define delegate for progress callback
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ProgressCallback(int percentage, double speedMbPs, int remainingSeconds);

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            LocalizationManager.Instance.LanguageChanged += OnLanguageChanged;
            LoadDrives();
        }

        private void LoadDrives()
        {
            AvailableDrives.Clear();

            // In a real application, we would use WMI (Win32_DiskDrive) to get actual PhysicalDrive numbers.
            // For this implementation, we will mock the physical drive mapping for removable drives.
            var drives = DriveInfo.GetDrives();
            int physicalDriveIndex = 1; // Assume 0 is the system drive

            foreach (var drive in drives)
            {
                // Safety Lock: ONLY allow Removable drives. NEVER Fixed (like C:\)
                if (drive.DriveType == DriveType.Removable && drive.IsReady)
                {
                    double sizeGb = drive.TotalSize / (1024.0 * 1024.0 * 1024.0);
                    AvailableDrives.Add($@"\\.\PhysicalDrive{physicalDriveIndex} - {drive.Name} ({sizeGb:F1} GB)");
                    physicalDriveIndex++;
                }
            }

            if (AvailableDrives.Count == 0)
            {
                AvailableDrives.Add("No Removable Drives Found");
            }

            DeviceComboBox.ItemsSource = AvailableDrives;
            DeviceComboBox.SelectedIndex = 0;
        }

        private void OnLanguageChanged(object sender, System.EventArgs e)
        {
            // Triggers automatically via INotifyPropertyChanged
        }

        public LocalizationManager LocalizedStrings => LocalizationManager.Instance;

        private void LanguageEN_Click(object sender, MouseButtonEventArgs e)
        {
            LocalizationManager.Instance.SetLanguage("en");
        }

        private void LanguageTR_Click(object sender, MouseButtonEventArgs e)
        {
            LocalizationManager.Instance.SetLanguage("tr");
        }

        private void SelectIsoButton_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder: Open file dialog
            IsoPathTextBox.Text = "ubuntu.iso";
            IsoPathTextBox.Foreground = System.Windows.Media.Brushes.White;
        }

        private async void FetchIsoButton_Click(object sender, RoutedEventArgs e)
        {
            if (CloudIsoComboBox.SelectedItem is ComboBoxItem item && item.Tag is string url)
            {
                StatusTextBlock.Text = "Fetching Cloud ISO...";
                StartButton.IsEnabled = false;

                try
                {
                    string fileName = Path.GetFileName(new System.Uri(url).LocalPath);
                    string tempPath = Path.Combine(Path.GetTempPath(), fileName);

                    // Simple download implementation (could be enhanced with progress reporting)
                    using (HttpClient client = new HttpClient())
                    {
                        // Use ResponseHeadersRead to prevent buffering the entire ISO in memory
                        using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                        {
                            response.EnsureSuccessStatusCode();

                            using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                await response.Content.CopyToAsync(fs);
                            }
                        }
                    }

                    IsoPathTextBox.Text = tempPath;
                    IsoPathTextBox.Foreground = System.Windows.Media.Brushes.White;
                    StatusTextBlock.Text = LocalizedStrings["status_ready"];

                    // Automatically start burning process as requested
                    StartButton_Click(null, null);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Failed to fetch ISO: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusTextBlock.Text = "Fetch Failed";
                    StartButton.IsEnabled = true;
                }
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsoPathTextBox.Text == "SELECT ISO IMAGE..." || string.IsNullOrEmpty(IsoPathTextBox.Text))
            {
                MessageBox.Show(LocalizedStrings["error_no_iso"], "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (DeviceComboBox.SelectedIndex < 0 || AvailableDrives.Count == 0 || AvailableDrives[0] == "No Removable Drives Found")
            {
                MessageBox.Show(LocalizedStrings["error_no_drive"], "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            StatusTextBlock.Text = LocalizedStrings["status_burning"];
            StartButton.IsEnabled = false;

            // Extract physical drive path from selection (e.g. "\\.\PhysicalDrive1 - E:\ (32.0 GB)" -> "\\.\PhysicalDrive1")
            string selectedDevice = DeviceComboBox.SelectedItem.ToString();
            string target = selectedDevice.Split(" - ")[0];

            // Define the callback
            ProgressCallback callback = (percentage, speed, remaining) =>
            {
                // Marshal back to UI thread
                Dispatcher.Invoke(() =>
                {
                    WriteProgressBar.Value = percentage;
                    SpeedTextBlock.Text = $"{speed:F1} MB/s";
                    TimeSpan time = TimeSpan.FromSeconds(remaining);
                    TimeTextBlock.Text = $"Remaining: {time.Minutes:D2}:{time.Seconds:D2}";
                });
            };

            // Run core engine function asynchronously
            string iso = IsoPathTextBox.Text;

            await Task.Run(() =>
            {
                // Call C-API via P/Invoke with the callback
                CoreInterop.WriteIsoAsync(target, iso, true, callback);
            });

            StatusTextBlock.Text = LocalizedStrings["status_done"];
            SpeedTextBlock.Text = "0 MB/s";
            TimeTextBlock.Text = "Remaining: 00:00";
            StartButton.IsEnabled = true;
        }
    }

    // P/Invoke Interop
    internal static class CoreInterop
    {
        private const string DllName = "diskform4th_core.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int WriteIsoAsync(string target, string isoPath, bool smartMonitor, MainWindow.ProgressCallback callback);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int FormatDisk(string target, bool quick, MainWindow.ProgressCallback callback);
    }
}