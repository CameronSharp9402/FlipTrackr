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
    public partial class SaleBreakdownWindow : Window
    {
        public SaleBreakdownWindow(InventoryItem item)
        {
            InitializeComponent();

            double netAmount = item.SellingPrice - item.Fees - item.Shipping;

            if (netAmount < 0)
            {
                NetTextBlock.Text = "Net Revenue: Negative (Check Fees/Shipping)";
                ReinvestTextBlock.Text = "Reinvest: N/A";
                PocketTextBlock.Text = "Pocket: N/A";
                EmergencyTextBlock.Text = "Emergency Fund: N/A";
                return;
            }

            double reinvest = netAmount * 0.60;
            double pocket = netAmount * 0.30;
            double emergency = netAmount * 0.10;

            NetTextBlock.Text = $"Net Revenue: {netAmount:C}";
            ReinvestTextBlock.Text = $"Reinvest: {reinvest:C}";
            PocketTextBlock.Text = $"Pocket: {pocket:C}";
            EmergencyTextBlock.Text = $"Emergency Fund: {emergency:C}";
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
