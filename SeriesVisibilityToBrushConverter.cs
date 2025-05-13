using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CSharpResaleBusinessTracker
{
    public class SeriesVisibilityToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var vm = value as SeriesViewModel;
            if (vm == null)
                return Brushes.Black;

            return vm.IsVisible ? vm.Series.Stroke : Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
