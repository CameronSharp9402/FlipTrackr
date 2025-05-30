using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FlipTrackr.Handlers
{
    public class SeriesViewModel : INotifyPropertyChanged
    {
        public string Title { get; set; }
        public LineSeries Series { get; set; }

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnPropertyChanged(nameof(IsVisible));
                    OnPropertyChanged(nameof(LegendColor)); // Notify when color changes
                }
            }
        }
        private bool _isVisible = true;

        public Brush LegendColor => IsVisible ? Brushes.Black : Brushes.Gray;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
