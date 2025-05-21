using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Globalization;
using System.Threading;

namespace CSharpResaleBusinessTracker
{
    public partial class SettingsWindow : Window
    {
        private readonly string themeSettingsPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FlipTrackr", "theme.txt");
        public double Threshold { get; private set; }

        private string selectedCultureCode = "en-US";
        public SettingsWindow(double currentThreshold)
        {
            InitializeComponent();
            Threshold = currentThreshold;
            LoadSettings();

            // Load from settings file (adjust this if you're using a different persistence approach)
            string savedCulture = Properties.Settings.Default.CurrencyCultureCode;
            if (!string.IsNullOrEmpty(savedCulture))
            {
                selectedCultureCode = savedCulture;

                // Select the matching ComboBoxItem
                foreach (ComboBoxItem item in CurrencyComboBox.Items)
                {
                    if (item.Tag.ToString() == selectedCultureCode)
                    {
                        CurrencyComboBox.SelectedItem = item;
                        break;
                    }
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

                    // Save the selected culture
                    Properties.Settings.Default.CurrencyCultureCode = selectedCultureCode;
                    Properties.Settings.Default.Save();

                    // Apply culture immediately
                    var culture = new CultureInfo(selectedCultureCode);
                    CultureInfo.DefaultThreadCurrentCulture = culture;
                    CultureInfo.DefaultThreadCurrentUICulture = culture;
                    Thread.CurrentThread.CurrentCulture = culture;
                    Thread.CurrentThread.CurrentUICulture = culture;

                    // Force UI to refresh with new formatting
                    mainWindow.UpdateDashboard();
                    mainWindow.RevenueCalculator();

                    MessageBox.Show("Settings saved successfully.");
                }

                Threshold = newThreshold;
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Please enter a valid number for ROI threshold.");
            }
        }

        private void LoadSettings()
        {
            RoiThreshold.Text = Threshold.ToString("F2");

            string savedCulture = Properties.Settings.Default.CurrencyCultureCode;
            if (!string.IsNullOrEmpty(savedCulture))
            {
                selectedCultureCode = savedCulture;

                foreach (ComboBoxItem item in CurrencyComboBox.Items)
                {
                    if (item.Tag?.ToString() == selectedCultureCode)
                    {
                        CurrencyComboBox.SelectedItem = item;
                        break;
                    }
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

            return "Light"; // default
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
