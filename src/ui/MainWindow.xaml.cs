using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;

namespace diskform4th.UI
{
    public partial class MainWindow : Window
    {
        // Define delegate for progress callback
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ProgressCallback(int percentage, double speedMbPs, int remainingSeconds);

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            LocalizationManager.Instance.LanguageChanged += OnLanguageChanged;
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
            IsoPathTextBox.Text = "C:\\Downloads\\ubuntu-24.04-desktop-amd64.iso";
            IsoPathTextBox.Foreground = System.Windows.Media.Brushes.White;
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsoPathTextBox.Text == "SELECT ISO IMAGE..." || string.IsNullOrEmpty(IsoPathTextBox.Text))
            {
                MessageBox.Show(LocalizedStrings["error_no_iso"], "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            StatusTextBlock.Text = LocalizedStrings["status_burning"];
            StartButton.IsEnabled = false;

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
            string target = "E:"; // Mock
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