using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace CSharpResaleBusinessTracker
{
    public class SafeImageConverter1 : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string filePath && File.Exists(filePath))
            {
                try
                {
                    var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
                    string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
                    if (Array.IndexOf(allowedExtensions, extension) >= 0)
                    {
                        var bitmap = new BitmapImage();
                        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.StreamSource = stream;
                            bitmap.EndInit();
                            bitmap.Freeze(); // Allow cross-thread access
                        }

                        return bitmap;
                    }
                }
                catch
                {
                    return DependencyProperty.UnsetValue;
                }
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
