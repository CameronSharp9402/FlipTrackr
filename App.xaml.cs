using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CSharpResaleBusinessTracker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string themePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FlipTrackr", "theme.txt");

            string theme = File.Exists(themePath)
                ? File.ReadAllText(themePath).Trim()
                : "Light";

            var uri = new Uri($"Theme/{theme}Theme.xaml", UriKind.Relative);
            Resources.MergedDictionaries.Add(new ResourceDictionary { Source = uri });
        }
    }
}
