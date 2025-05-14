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

namespace CSharpResaleBusinessTracker
{
    public partial class SettingsWindow : Window
    {
        private readonly string themeSettingsPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FlipTrackr", "theme.txt");
        public double Threshold { get; private set; }
        public SettingsWindow(double currentThreshold)
        {
            InitializeComponent();
            RoiThreshold.Text = currentThreshold.ToString("F2");
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(RoiThreshold.Text, out double newThreshold))
            {
                // Save the new threshold to MainWindow
                if (Owner is MainWindow mainWindow)
                {
                    mainWindow.UpdateRoiThreshold(newThreshold);
                }

                // Set DialogResult and close
                Threshold = newThreshold;
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Please enter a valid number for ROI threshold.");
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

    }
}
