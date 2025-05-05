using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace CSharpResaleBusinessTracker
{
    public class ImageVisibilityConverter : IValueConverter
    {
        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string filePath && File.Exists(filePath))
            {
                string extension = Path.GetExtension(filePath)?.ToLowerInvariant();
                return Array.Exists(ImageExtensions, ext => ext == extension) ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}

