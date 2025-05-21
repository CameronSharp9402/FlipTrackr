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
    public partial class TOSAgreementWindow : Window
    {
        public bool Accepted { get; private set; } = false;

        public TOSAgreementWindow()
        {
            InitializeComponent();
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            Accepted = true;
            this.DialogResult = true;
        }

        private void Decline_Click(object sender, RoutedEventArgs e)
        {
            Accepted = false;
            this.DialogResult = false;
        }

        private void ViewTerms_Click(object sender, RoutedEventArgs e)
        {
            TermsOfServiceWindow tosWindow = new TermsOfServiceWindow();
            tosWindow.Owner = this;
            tosWindow.ShowDialog();
        }
    }
}
