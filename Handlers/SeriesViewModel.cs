using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CSharpResaleBusinessTracker
{
    public class SeriesViewModel : INotifyPropertyChanged
    {
        private bool isVisible = true;

        public string Title { get; set; }
        public LineSeries Series { get; set; }

        public bool IsVisible
        {
            get => isVisible;
            set
            {
                if (isVisible != value)
                {
                    isVisible = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LegendColor));
                }
            }
        }

        public Brush LegendColor => IsVisible ? Series.Stroke : Brushes.Gray;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
