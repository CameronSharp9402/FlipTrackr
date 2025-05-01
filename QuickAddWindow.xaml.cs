using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CSharpResaleBusinessTracker
{
    public partial class QuickAddWindow : Window
    {
        private ObservableCollection<InventoryItem> inventoryReference;

        public QuickAddWindow(ObservableCollection<InventoryItem> inventory)
        {
            InitializeComponent();
            inventoryReference = inventory;

            // Setup dropdowns
            MarketplaceComboBox.ItemsSource = InventoryItem.MarketplaceOptions.Prepend("Select Marketplace");
            CategoryComboBox.ItemsSource = ((MainWindow)Application.Current.MainWindow).Categories;
            StageComboBox.ItemsSource = new[] { "Select Stage", "Sourced", "Listed", "Sold", "Returned", "Pending" };

            // Default
            MarketplaceComboBox.SelectedIndex = 0;
            CategoryComboBox.SelectedIndex = 0;
            StageComboBox.SelectedIndex = 0;
        }

        private void AddItems_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(PurchasePriceBox.Text, out double purchasePrice) ||
                !double.TryParse(SellingPriceBox.Text, out double sellingPrice) ||
                string.IsNullOrWhiteSpace(ItemNameBox.Text) ||
                string.IsNullOrWhiteSpace(SkuBox.Text) ||
                !DatePurchasedPicker.SelectedDate.HasValue ||
                MarketplaceComboBox.SelectedIndex == 0 ||
                CategoryComboBox.SelectedIndex == 0 ||
                StageComboBox.SelectedIndex == 0)
            {
                MessageBox.Show("Please enter valid item info.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int quantity = int.TryParse(QuantityBox.Text, out int q) && q > 0 ? q : 1;

            for (int i = 0; i < quantity; i++)
            {
                var newItem = new InventoryItem
                {
                    ItemName = ItemNameBox.Text,
                    PurchasePrice = purchasePrice,
                    SellingPrice = sellingPrice,
                    SKU = SkuBox.Text + (quantity > 1 ? $"-{i + 1}" : ""),  // If batching, auto-suffix SKU
                    DatePurchased = DatePurchasedPicker.SelectedDate.Value.ToString("MM/dd/yyyy"),
                    MarketplaceIndex = MarketplaceComboBox.SelectedIndex,
                    Category = CategoryComboBox.SelectedItem.ToString(),
                    LifecycleIndex = StageComboBox.SelectedIndex,
                    Tags = "",   // Optional: you can add a Tags field if needed
                    IsSold = false
                };

                inventoryReference.Add(newItem);
                DatabaseHelper.AddInventoryItem(newItem);
            }

            ((MainWindow)Application.Current.MainWindow).UpdateDashboard();
            this.Close();
        }
    }
}

