using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace diskform4th.UI
{
    public class LocalizationManager : System.ComponentModel.INotifyPropertyChanged
    {
        private static LocalizationManager _instance;
        private Dictionary<string, string> _strings = new Dictionary<string, string>();
        private string _currentLanguage = "en";

        public event EventHandler LanguageChanged;
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public static LocalizationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LocalizationManager();
                }
                return _instance;
            }
        }

        private LocalizationManager()
        {
            LoadLanguage(_currentLanguage);
        }

        public void SetLanguage(string langCode)
        {
            if (_currentLanguage != langCode)
            {
                _currentLanguage = langCode;
                LoadLanguage(langCode);
                LanguageChanged?.Invoke(this, EventArgs.Empty);
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
            }
        }

        private void LoadLanguage(string langCode)
        {
            try
            {
                // In a real app, this path would be relative to the executable directory
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "locales", $"{langCode}.json");

                // Fallback for development/testing if run directly from source tree
                if (!File.Exists(filePath))
                {
                    filePath = Path.Combine("..", "..", "..", "..", "locales", $"{langCode}.json");
                }

                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    _strings = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                }
                else
                {
                    Console.WriteLine($"Localization file not found: {filePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading localization file: {ex.Message}");
            }
        }

        public string this[string key]
        {
            get
            {
                if (_strings.TryGetValue(key, out string value))
                {
                    return value;
                }
                return $"[{key}]"; // Placeholder if string not found
            }
        }
    }
}