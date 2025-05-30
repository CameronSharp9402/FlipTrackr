using FlipTrackr.Handlers;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace CSharpResaleBusinessTracker
{
    public partial class SettingsWindow : Window
    {
        private readonly string themeSettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FlipTrackr", "theme.txt");

        private string selectedCultureCode = "en-US";
        public double Threshold { get; private set; }

        public SettingsWindow(double currentThreshold)
        {
            InitializeComponent();
            Threshold = currentThreshold;
            LoadSettings();

            // Load and apply saved culture
            selectedCultureCode = SettingsManager.CurrentSettings.CurrencyCultureCode;

            foreach (ComboBoxItem item in CurrencyComboBox.Items)
            {
                if (item.Tag?.ToString() == selectedCultureCode)
                {
                    CurrencyComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(RoiThreshold.Text, out double newThreshold))
            {
                if (Owner is MainWindow mainWindow)
                {
                    mainWindow.UpdateRoiThreshold(newThreshold);

                    // Update and save selected culture
                    SettingsManager.CurrentSettings.CurrencyCultureCode = selectedCultureCode;
                    SettingsManager.SaveSettings();

                    // Apply culture immediately
                    var culture = new CultureInfo(selectedCultureCode);
                    CultureInfo.DefaultThreadCurrentCulture = culture;
                    CultureInfo.DefaultThreadCurrentUICulture = culture;
                    Thread.CurrentThread.CurrentCulture = culture;
                    Thread.CurrentThread.CurrentUICulture = culture;

                    mainWindow.UpdateDashboard();
                    mainWindow.RevenueCalculator();

                    MessageBox.Show("Settings saved successfully.");
                }

                Threshold = newThreshold;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please enter a valid number for ROI threshold.");
            }
        }

        private void LoadSettings()
        {
            RoiThreshold.Text = Threshold.ToString("F2");

            selectedCultureCode = SettingsManager.CurrentSettings.CurrencyCultureCode;

            foreach (ComboBoxItem item in CurrencyComboBox.Items)
            {
                if (item.Tag?.ToString() == selectedCultureCode)
                {
                    CurrencyComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void ToggleDarkMode_Click(object sender, RoutedEventArgs e)
        {
            string newTheme = ApplyTheme(GetCurrentTheme() == "Dark" ? "Light" : "Dark");
            File.WriteAllText(themeSettingsPath, newTheme);
            MessageBox.Show($"{newTheme} mode applied. Restart app to fully apply theme.");
        }

        private string ApplyTheme(string themeName)
        {
            var dictionaries = Application.Current.Resources.MergedDictionaries;
            dictionaries.Clear();

            var themeUri = new Uri($"Theme/{themeName}Theme.xaml", UriKind.Relative);
            dictionaries.Add(new ResourceDictionary { Source = themeUri });

            return themeName;
        }

        private string GetCurrentTheme()
        {
            if (File.Exists(themeSettingsPath))
                return File.ReadAllText(themeSettingsPath).Trim();

            return "Light"; // Default
        }

        private void CurrencyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CurrencyComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                selectedCultureCode = selectedItem.Tag.ToString();
            }
        }
    }
}
