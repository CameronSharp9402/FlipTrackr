using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FlipTrackr.Handlers
{
    public class AppSettings
    {
        public string CurrencyCultureCode { get; set; } = CultureInfo.CurrentCulture.Name;
        public double RoiThreshold { get; set; } = 20.0;
    }

    public static class SettingsManager
    {
        public static AppSettings CurrentSettings { get; private set; } = new AppSettings();

        private static readonly string settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FlipTrackr", "settings.json");

        public static void LoadSettings()
        {
            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                CurrentSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }

        public static void SaveSettings()
        {
            var json = JsonSerializer.Serialize(CurrentSettings);
            File.WriteAllText(settingsPath, json);
        }
    }
}
