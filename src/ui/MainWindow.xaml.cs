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
using System.Windows.Threading;

namespace diskform4th.UI
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<string> AvailableDrives { get; set; } = new ObservableCollection<string>();

        // Define delegate for progress callback
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ProgressCallback(int percentage, double speedMbPs, int remainingSeconds, int temperature, bool healthy, [MarshalAs(UnmanagedType.LPStr)] string hashStr);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct SecurityConfig
        {
            [MarshalAs(UnmanagedType.LPStr)] public string outer_pass;
            [MarshalAs(UnmanagedType.LPStr)] public string inner_pass;
            public bool enable_hidden_vol;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ProgressData
        {
            public int percentage;
            public double speedMbPs;
            public int remainingSeconds;
            public int temperature;
            public bool healthy;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string hashStr;
        }

        private DispatcherTimer _pollTimer;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            LocalizationManager.Instance.LanguageChanged += OnLanguageChanged;
            LoadDrives();

            _pollTimer = new DispatcherTimer();
            _pollTimer.Interval = TimeSpan.FromMilliseconds(50);
            _pollTimer.Tick += PollTimer_Tick;
        }

        private void PollTimer_Tick(object? sender, EventArgs e)
        {
            ProgressData data = new ProgressData();
            while (CoreInterop.PollProgress(ref data))
            {
                WriteProgressBar.Value = data.percentage;
                if (data.speedMbPs == -1.5) {
                    SpeedTextBlock.Text = "QR READY";
                    SpeedTextBlock.Foreground = System.Windows.Media.Brushes.LightGreen;
                    if (!string.IsNullOrEmpty(data.hashStr)) {
                        MessageBox.Show($"Validation Hash:\n{data.hashStr}", "Air-Gapped Validation Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    } else {
                        MessageBox.Show("QR Validation Payload", "Air-Gapped Validation QR", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                } else if (data.speedMbPs < 0.0) {
                    SpeedTextBlock.Text = "PROCESSING...";
                    SpeedTextBlock.Foreground = System.Windows.Media.Brushes.MediumPurple;
                } else {
                    SpeedTextBlock.Text = $"{data.speedMbPs:F1} MB/s";
                    SpeedTextBlock.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0078D7"));
                }

                TimeSpan time = TimeSpan.FromSeconds(data.remainingSeconds);
                TimeTextBlock.Text = $"Remaining: {time.Minutes:D2}:{time.Seconds:D2}";
            }
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

        private bool _win11BypassRequested = false;

        private void SelectIsoButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Disk Images (*.iso;*.vhdx;*.vhd;*.vmdk)|*.iso;*.vhdx;*.vhd;*.vmdk|All files (*.*)|*.*";
            openFileDialog.Title = "Select Boot Image";

            if (openFileDialog.ShowDialog() == true)
            {
                IsoPathTextBox.Text = openFileDialog.FileName;
                IsoPathTextBox.Foreground = System.Windows.Media.Brushes.White;
                CheckForWindows11Bypass(openFileDialog.FileName);
            }
        }

        private void CheckForWindows11Bypass(string isoName)
        {
            _win11BypassRequested = false;
            if (isoName.Contains("Win11", System.StringComparison.OrdinalIgnoreCase) || isoName.Contains("Windows11", System.StringComparison.OrdinalIgnoreCase))
            {
                var result = MessageBox.Show(
                    "Windows 11 image detected.\n\nWould you like to automatically inject a bypass for TPM, RAM, and Secure Boot restrictions during the burn process?",
                    "Remove Windows 11 Restrictions",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _win11BypassRequested = true;
                    StatusTextBlock.Text = "Win11 Bypass queued for burn phase.";
                }
            }
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

        private async void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            if (DeviceComboBox.SelectedIndex < 0 || AvailableDrives.Count == 0 || AvailableDrives[0] == "No Removable Drives Found")
            {
                MessageBox.Show(LocalizedStrings["error_no_drive"], "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Filter = "Compressed Disk Image (*.img.gz)|*.img.gz";
            saveFileDialog.Title = "Save Reverse Clone Backup";
            saveFileDialog.FileName = "backup.img.gz";

            if (saveFileDialog.ShowDialog() != true)
            {
                return;
            }

            string savePath = saveFileDialog.FileName;

            StatusTextBlock.Text = $"Backing up drive to {savePath}...";
            StartButton.IsEnabled = false;
            BackupButton.IsEnabled = false;
            if (PxeServerButton != null) PxeServerButton.IsEnabled = false;

            string selectedDevice = DeviceComboBox.SelectedItem.ToString();
            string target = selectedDevice.Split(" - ")[0];

            ProgressCallback callback = (percentage, speed, remaining, temp, healthy, hashStr) =>
            {
                Dispatcher.Invoke(() =>
                {
                    WriteProgressBar.Value = percentage;
                    SpeedTextBlock.Text = $"{speed:F1} MB/s";
                    SpeedTextBlock.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0078D7"));
                    TimeSpan time = TimeSpan.FromSeconds(remaining);
                    TimeTextBlock.Text = $"Remaining: {time.Minutes:D2}:{time.Seconds:D2}";
                });
            };

            await Task.Run(() =>
            {
                CoreInterop.BackupDriveAsync(target, savePath, callback);
            });

            StatusTextBlock.Text = "Backup Complete.";
            SpeedTextBlock.Text = "0 MB/s";
            StartButton.IsEnabled = true;
            BackupButton.IsEnabled = true;
            if (PxeServerButton != null) PxeServerButton.IsEnabled = true;
        }

        private async void PxeServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsoPathTextBox.Text == "SELECT ISO IMAGE..." || string.IsNullOrEmpty(IsoPathTextBox.Text))
            {
                MessageBox.Show(LocalizedStrings["error_no_iso"], "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            StatusTextBlock.Text = "Starting PXE Server...";
            if (PxeServerButton != null) PxeServerButton.IsEnabled = false;
            StartButton.IsEnabled = false;
            BackupButton.IsEnabled = false;

            ProgressCallback callback = (percentage, speed, remaining, temp, healthy, hashStr) =>
            {
                Dispatcher.Invoke(() =>
                {
                    WriteProgressBar.Value = percentage;
                    SpeedTextBlock.Text = "PXE SERVING";
                    SpeedTextBlock.Foreground = System.Windows.Media.Brushes.MediumPurple;
                });
            };

            string iso = IsoPathTextBox.Text;

            await Task.Run(() =>
            {
                CoreInterop.StartPxeServer(iso, callback);
            });

            StatusTextBlock.Text = "PXE Server Stopped.";
            if (PxeServerButton != null) PxeServerButton.IsEnabled = true;
            StartButton.IsEnabled = true;
            BackupButton.IsEnabled = true;
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
            if (PxeServerButton != null) PxeServerButton.IsEnabled = false;
            BackupButton.IsEnabled = false;

            // Extract physical drive path from selection (e.g. "\\.\PhysicalDrive1 - E:\ (32.0 GB)" -> "\\.\PhysicalDrive1")
            string selectedDevice = DeviceComboBox.SelectedItem.ToString();
            string target = selectedDevice.Split(" - ")[0];

            // Define the callback
            ProgressCallback callback = (percentage, speed, remaining, temp, healthy, hashStr) =>
            {
                // Marshal back to UI thread
                Dispatcher.Invoke(() =>
                {
                    WriteProgressBar.Value = percentage;

                    if (speed == -1.5) {
                        // Special signal for Air-Gapped QR Validation
                        SpeedTextBlock.Text = "QR READY";
                        SpeedTextBlock.Foreground = System.Windows.Media.Brushes.LightGreen;

                        // Render mock QR payload box using ASCII string representation format matching CLI
                        string qr =
                        "  ██████████████  ██████    ████  ██████████████  \n" +
                        "  ██          ██  ████  ████  ██  ██          ██  \n" +
                        "  ██  ██████  ██      ████  ██    ██  ██████  ██  \n" +
                        "  ██  ██████  ██  ██    ██  ████  ██  ██████  ██  \n" +
                        "  ██  ██████  ██    ██████████    ██  ██████  ██  \n" +
                        "  ██          ██  ██  ██  ████    ██          ██  \n" +
                        "  ██████████████  ██  ██  ██  ██  ██████████████  \n" +
                        "                  ████  ██████                    \n" +
                        "  ████    ████████    ██  ██████    ██  ████  ██  \n" +
                        "      ██████  ██  ████    ████  ████████  ████    \n" +
                        "  ████████    ████  ████  ████████      ██  ████  \n" +
                        "  ██      ██████████  ██  ██████  ██  ██████████  \n" +
                        "  ████  ██████  ██  ████████████  ██  ██    ██    \n" +
                        "                  ██    ████████████    ██  ████  \n" +
                        "  ██████████████    ██  ██    ██████████  ██      \n" +
                        "  ██          ██  ████    ██████  ████    ████    \n" +
                        "  ██  ██████  ██      ██████  ██    ██████        \n" +
                        "  ██  ██████  ██  ██      ████  ████      ██████  \n" +
                        "  ██  ██████  ██  ██████████  ██    ████████      \n" +
                        "  ██          ██  ██    ██████  ██    ████  ████  \n" +
                        "  ██████████████  ██████████  ████  ████  ████    \n";

                        if (!string.IsNullOrEmpty(hashStr)) {
                            // Instead of a console ASCII block, UI renders hash here.
                            MessageBox.Show($"Validation Hash:\n{hashStr}", "Air-Gapped Validation Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        } else {
                            MessageBox.Show(qr, "Air-Gapped Validation QR", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    } else if (speed < 0.0) {
                        // Special signal for Verification phase
                        SpeedTextBlock.Text = "VERIFYING...";
                        SpeedTextBlock.Foreground = System.Windows.Media.Brushes.MediumPurple;
                    } else {
                        SpeedTextBlock.Text = $"{speed:F1} MB/s";
                        SpeedTextBlock.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0078D7"));
                    }

                    TimeSpan time = TimeSpan.FromSeconds(remaining);
                    TimeTextBlock.Text = $"Remaining: {time.Minutes:D2}:{time.Seconds:D2}";

                    // Update S.M.A.R.T. Temperature Display
                    TempTextBlock.Text = $"{temp}°C";
                    if (temp >= 60 || !healthy) {
                        TempTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                    } else {
                        TempTextBlock.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0078D7"));
                    }
                });
            };

            // Run core engine function asynchronously
            string iso = IsoPathTextBox.Text;
            bool isIsoMode = ImageModeComboBox.SelectedIndex == 0 || ImageModeComboBox.SelectedIndex == 2; // Treat Multi-Boot as an ISO mode extension
            bool isMultiBoot = ImageModeComboBox.SelectedIndex == 2;
            bool verify = VerifyCheckBox.IsChecked ?? false;
            bool preLoadRam = PreloadRamCheckBox.IsChecked ?? false;
            bool secureErase = SecureEraseCheckBox.IsChecked ?? false;
            bool encryptSpace = EncryptSpaceCheckBox.IsChecked ?? false;
            bool persistence = PersistenceCheckBox.IsChecked ?? false;
            bool lockDrive = LockDriveCheckBox.IsChecked ?? false;

            _pollTimer.Start();

            await Task.Run(() =>
            {
                SecurityConfig secConfig = new SecurityConfig {
                    outer_pass = "outer",
                    inner_pass = "inner",
                    enable_hidden_vol = encryptSpace
                };

                // Call C-API via P/Invoke with null callback to rely purely on polling
                CoreInterop.WriteIsoAsync(target, iso, isIsoMode, true, verify, preLoadRam, secureErase, encryptSpace, persistence, isMultiBoot, ref secConfig, null);

                // Perform Win11 injection natively after the main burn process completes
                if (_win11BypassRequested)
                {
                    Dispatcher.Invoke(() => { StatusTextBlock.Text = "Injecting Win11 Bypasses..."; });
                    CoreInterop.InjectWin11Bypass(target, null);
                }

                // Lock drive firmly as the final step via SCSI passthrough
                if (lockDrive)
                {
                    Dispatcher.Invoke(() => { StatusTextBlock.Text = "Locking Drive (Read-Only)..."; });
                    CoreInterop.LockDriveReadOnly(target, null);
                }
            });

            _pollTimer.Stop();
            StatusTextBlock.Text = LocalizedStrings["status_done"];
            SpeedTextBlock.Text = "0 MB/s";
            TimeTextBlock.Text = "Remaining: 00:00";
            StartButton.IsEnabled = true;
            if (PxeServerButton != null) PxeServerButton.IsEnabled = true;
            BackupButton.IsEnabled = true;
        }
    }

    // P/Invoke Interop
    internal static class CoreInterop
    {
        private const string DllName = "diskform4th_core.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern bool PollProgress(ref MainWindow.ProgressData outData);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int WriteIsoAsync(string target, string isoPath, bool isIsoMode, bool smartMonitor, bool verifyBlocks, bool preLoadRam, bool secureErase, bool encryptSpace, bool persistence, bool multiBoot, ref MainWindow.SecurityConfig secConfig, MainWindow.ProgressCallback? callback);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int FormatDisk(string target, bool quick, MainWindow.ProgressCallback? callback);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int StartPxeServer(string isoPath, MainWindow.ProgressCallback? callback);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int InjectWin11Bypass(string target, MainWindow.ProgressCallback? callback);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int BackupDriveAsync(string sourceDrive, string targetImagePath, MainWindow.ProgressCallback? callback);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int LockDriveReadOnly(string target, MainWindow.ProgressCallback? callback);
    }
}