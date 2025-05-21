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
    /// <summary>
    /// Interaction logic for ReportBugWindow.xaml
    /// </summary>
    public partial class ReportBugWindow : Window
    {
        public ReportBugWindow()
        {
            InitializeComponent();
        }
        private void SubmitBugReport_Click(object sender, RoutedEventArgs e)
        {
            string bugDetails = Uri.EscapeDataString(BugDescriptionTextBox.Text);
            if (!string.IsNullOrWhiteSpace(bugDetails))
            {
                string subject = Uri.EscapeDataString("Bug Report - FlipTrackr");
                string mailto = $"mailto:CameronSharp9402@aol.com?subject={subject}&body={bugDetails}";

                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(mailto) { UseShellExecute = true });
                    MessageBox.Show("Your default email client has been opened with the bug report.", "Email Ready", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open email client. {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please enter a description of the bug before submitting.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
