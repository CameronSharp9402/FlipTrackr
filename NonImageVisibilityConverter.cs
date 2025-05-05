using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace CSharpResaleBusinessTracker
{
    public class NonImageVisibilityConverter : IValueConverter
    {
        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string filePath && File.Exists(filePath))
            {
                string ext = Path.GetExtension(filePath).ToLowerInvariant();
                return Array.Exists(ImageExtensions, e => e == ext) ? Visibility.Collapsed : Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}