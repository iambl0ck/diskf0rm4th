using System.Windows;
using System.Windows.Controls;

namespace diskform4th.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            LocalizationManager.Instance.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, System.EventArgs e)
        {
            // Trigger binding update for localized strings
            // We rely on the LocalizationManager's PropertyChanged event for "Item[]" instead
        }

        public LocalizationManager LocalizedStrings => LocalizationManager.Instance;

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is ComboBoxItem item && item.Tag is string langCode)
            {
                LocalizationManager.Instance.SetLanguage(langCode);
            }
        }

        private void SelectIsoButton_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder: Open file dialog
            IsoPathTextBox.Text = "C:\\Downloads\\ubuntu-24.04-desktop-amd64.iso";
        }

        private void FormatButton_Click(object sender, RoutedEventArgs e)
        {
            StatusTextBlock.Text = LocalizationManager.Instance["status_formatting"];
            // Hook into Core Engine later
        }

        private void BurnButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(IsoPathTextBox.Text))
            {
                MessageBox.Show(LocalizationManager.Instance["error_no_iso"], "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            StatusTextBlock.Text = LocalizationManager.Instance["status_burning"];
            // Hook into Core Engine later
        }

    }
}