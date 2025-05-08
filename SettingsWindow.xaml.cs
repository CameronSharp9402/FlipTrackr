using System;
using System.Collections.Generic;
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
    }

}
