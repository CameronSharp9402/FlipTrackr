using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using System.Threading;

namespace CSharpResaleBusinessTracker
{
    public partial class MainWindow : Window
    {
        #region Initialization

        private ObservableCollection<InventoryItem> inventory;
        private ObservableCollection<Expenses> expense;
        public ObservableCollection<string> Categories { get; set; }
        public string SelectedCategory { get; set; }

        private DateTime? startDateFilter = null;
        private DateTime? endDateFilter = null;

        public static MainWindow Current => Application.Current.MainWindow as MainWindow;

        public double RoiThreshold { get; set; }
        public double YellowLeeway { get; set; } = 5;   // ±5% around threshold

        private readonly string AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FlipTrackr");

        public ObservableCollection<SeriesViewModel> DashboardSeriesViewModels { get; set; } = new ObservableCollection<SeriesViewModel>();
        public SeriesCollection DashboardSeries { get; set; } = new SeriesCollection();
        public ObservableCollection<string> DashboardLabels { get; set; } = new ObservableCollection<string>();
        public Func<double, string> XValueFormatter { get; set; }
        public Func<double, string> CurrencyFormatter { get; set; }

        private DateTime? reportFilterStartDate = null;
        private DateTime? reportFilterEndDate = null;
        private Dictionary<string, bool> seriesVisibility = new Dictionary<string, bool>();

        public MainWindow()
        {
            SQLitePCL.Batteries_V2.Init();

            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlipTrackr");
            string tosPath = Path.Combine(appDataPath, "tos.txt");

            if (!File.Exists(tosPath) || File.ReadAllText(tosPath).Trim().ToLower() != "accepted")
            {
                var tosWindow = new TOSAgreementWindow();
                bool? result = tosWindow.ShowDialog();

                if (result != true || !tosWindow.Accepted)
                {
                    MessageBox.Show("You must accept the Terms of Service to use FlipTrackr.", "Agreement Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Application.Current.Shutdown();
                    return;
                }

                // Save TOS acceptance
                Directory.CreateDirectory(appDataPath);
                File.WriteAllText(tosPath, "accepted");
            }

            XValueFormatter = value => new DateTime((long)value).ToString("MM / dd / yyyy");
            DashboardLabels = new ObservableCollection<string>();
            CurrencyFormatter = value => value.ToString("C", CultureInfo.CurrentCulture); // $0.00
            var rawSeriesList = new List<SeriesViewModel>
{
                new SeriesViewModel
                {
                    Title = "Revenue",
                    Series = new LineSeries
                    {
                        Title = "Revenue",
                        Values = new ChartValues<DateTimePoint>(),
                        LineSmoothness = 1,
                        PointGeometrySize = 8,
                        Stroke = Brushes.SteelBlue
                    }
                },
                new SeriesViewModel
                {
                    Title = "Cost",
                    Series = new LineSeries
                    {
                        Title = "Cost",
                        Values = new ChartValues<DateTimePoint>(),
                        LineSmoothness = 1,
                        PointGeometrySize = 8,
                        Stroke = Brushes.IndianRed
                    }
                },
                new SeriesViewModel
                {
                    Title = "Profit",
                    Series = new LineSeries
                    {
                        Title = "Profit",
                        Values = new ChartValues<DateTimePoint>(),
                        LineSmoothness = 1,
                        PointGeometrySize = 8,
                        Stroke = Brushes.DarkGreen
                    }
                },
                new SeriesViewModel
                {
                    Title = "Shipping",
                    Series = new LineSeries
                    {
                        Title = "Shipping",
                        Values = new ChartValues<DateTimePoint>(),
                        LineSmoothness = 1,
                        PointGeometrySize = 8,
                        Stroke = Brushes.MediumPurple
                    }
                },
                new SeriesViewModel
                {
                    Title = "Fees",
                    Series = new LineSeries
                    {
                        Title = "Fees",
                        Values = new ChartValues<DateTimePoint>(),
                        LineSmoothness = 1,
                        PointGeometrySize = 8,
                        Stroke = Brushes.Orange
                    }
                }
            };

            foreach (var vm in rawSeriesList)
            {
                DashboardSeries.Add(vm.Series);
                DashboardSeriesViewModels.Add(vm);
                seriesVisibility[vm.Title] = true;
            }

            InitializeComponent();

            var settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlipTrackr", "settings.txt");
            if (File.Exists(settingsPath) &&
                double.TryParse(File.ReadAllText(settingsPath), NumberStyles.Float, CultureInfo.InvariantCulture, out double savedThreshold))
            {
                RoiThreshold = savedThreshold;
            }
            else
            {
                RoiThreshold = 20; // Default fallback
            }

            // Initialize ObservableCollections
            Categories = new ObservableCollection<string>();
            inventory = new ObservableCollection<InventoryItem>();
            expense = new ObservableCollection<Expenses>();

            // Load categories from database
            var loadedCategories = DatabaseHelper.LoadCategories();
            Categories.Add("Select Category");
            foreach (var cat in loadedCategories)
            {
                Categories.Add(cat);
            }
            Categories.Add("Add new...");

            SelectedCategory = Categories[0];

            // Load inventory and expenses
            var items = DatabaseHelper.LoadInventoryItems();
            foreach (var item in items)
            {
                item.PropertyChanged += InventoryItem_PropertyChanged;
            }
            inventory = new ObservableCollection<InventoryItem>(items);
            expense = new ObservableCollection<Expenses>(DatabaseHelper.LoadExpenseItems());

            string cultureCode = Properties.Settings.Default.CurrencyCultureCode;
            if (!string.IsNullOrEmpty(cultureCode))
            {
                var culture = new CultureInfo(cultureCode);
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
            }

            // Set item sources
            InventoryTable.ItemsSource = inventory;
            ExpenseTable.ItemsSource = expense;

            // Set initial UI state
            DateBox.SelectedDate = null;
            ExpenseDateBox.SelectedDate = null;

            // Hook up collection changed events
            inventory.CollectionChanged += Inventory_CollectionChanged;
            expense.CollectionChanged += Expense_CollectionChanged;

            // Set default filter to Today on startup
            startDateFilter = DateTime.Now.Date;
            endDateFilter = DateTime.Now.Date.AddDays(1);

            // Set DataContext last to ensure everything is ready
            this.DataContext = this;

            UpdateDashboard();
        }
        #endregion

        #region Event Handlers
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            InventoryTable.CommitEdit(DataGridEditingUnit.Row, true);
        }

        private void Inventory_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (InventoryItem item in e.NewItems)
                {
                    item.PropertyChanged += InventoryItem_PropertyChanged;
                }
            }

            // Always update the dashboard whenever the collection changes
            UpdateDashboard();
        }

        private void Expense_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (Expenses item in e.NewItems)
                {
                    item.PropertyChanged += ExpenseItem_PropertyChanged;
                }
            }

            UpdateDashboard();
        }

        private void InventoryItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Update the dashboard immediately when any of these properties change
            if (e.PropertyName == nameof(InventoryItem.IsSold) ||
                e.PropertyName == nameof(InventoryItem.SellingPrice) ||
                e.PropertyName == nameof(InventoryItem.PurchasePrice) ||
                e.PropertyName == nameof(InventoryItem.MarketplaceIndex))
            {
                UpdateDashboard();
            }

            // Always update the database when a change is made
            var item = (InventoryItem)sender;
            DatabaseHelper.UpdateInventoryItem(item);
        }

        private void ExpenseItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Expenses.PurchasePrice))
            {
                UpdateDashboard();
            }

            var item = (Expenses)sender;
            DatabaseHelper.UpdateExpenseItem(item);
        }

        private void InventoryTable_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Ensure to save updated values back to the database
            var editedItem = (InventoryItem)e.Row.Item;
            DatabaseHelper.UpdateInventoryItem(editedItem);

            UpdateDashboard();
        }

        private void ExpenseTable_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            var editedItem = (Expenses)e.Row.Item;
            DatabaseHelper.UpdateExpenseItem(editedItem);

            // Update dashboard immediately after any change in the expense table
            UpdateDashboard();
        }

        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryComboBox.SelectedItem?.ToString() == "Add new...")
            {
                var inputDialog = new InputDialog("Enter new category:");
                if (inputDialog.ShowDialog() == true)
                {
                    string newCategory = inputDialog.ResponseText.Trim();

                    if (!string.IsNullOrWhiteSpace(newCategory) && !Categories.Contains(newCategory))
                    {
                        // Save to DB
                        DatabaseHelper.AddCategory(newCategory);

                        // Add new category before "Add new..."
                        Categories.Insert(Categories.Count - 1, newCategory);
                        CategoryComboBox.SelectedItem = newCategory;
                    }
                    else
                    {
                        CategoryComboBox.SelectedIndex = 0;
                    }
                }
                else
                {
                    CategoryComboBox.SelectedIndex = 0;
                }
            }
        }

        private void ManageCategories_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ManageCategoriesDialog(Categories);
            dialog.Owner = this;
            dialog.Closed += ManageCategoriesDialog_Closed;
            dialog.ShowDialog();

            // Refresh categories from database after closing
            var updated = DatabaseHelper.LoadCategories();
            Categories.Clear();
            Categories.Add("Select Category");
            foreach (var cat in updated)
                Categories.Add(cat);
            Categories.Add("Add new...");

            // Set both the selected item property and the ComboBox index
            SelectedCategory = Categories[0];
            CategoryComboBox.SelectedIndex = 0; // <-- this line makes the UI show "Select Category"
        }

        public void ResetCategorySelection()
        {
            CategoryComboBox.SelectedIndex = 0; // Reset category selection to "Select Category"
        }

        private void ManageCategoriesDialog_Closed(object sender, EventArgs e)
        {
            ResetCategorySelection();  // Reset category combo box to default
        }

        private void LifecycleTableComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var selectedItem = (InventoryItem)comboBox.DataContext;

            if (selectedItem != null && comboBox.SelectedIndex >= 0)
            {
                selectedItem.LifecycleIndex = comboBox.SelectedIndex;
                DatabaseHelper.UpdateInventoryItem(selectedItem); // Assuming this saves to DB
                UpdateDashboard(); // <-- Trigger immediate UI update
            }
        }
        private void ItemConditionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var selectedItem = (InventoryItem)comboBox.DataContext;

            if (selectedItem != null && comboBox.SelectedIndex >= 0)
            {
                selectedItem.ItemConditionIndex = comboBox.SelectedIndex;
                DatabaseHelper.UpdateInventoryItem(selectedItem); // Assuming this saves to DB
            }
        }
        private void ShippingMethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var selectedItem = (InventoryItem)comboBox.DataContext;

            if (selectedItem != null && comboBox.SelectedIndex >= 0)
            {
                selectedItem.ShippingMethodIndex = comboBox.SelectedIndex;
                DatabaseHelper.UpdateInventoryItem(selectedItem); // Assuming this saves to DB
            }
        }
        public void UpdateRoiThreshold(double newThreshold)
        {
            RoiThreshold = newThreshold;

            // Save it to file
            var settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlipTrackr", "settings.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
            File.WriteAllText(settingsPath, newThreshold.ToString(CultureInfo.InvariantCulture));

            // Immediately update
            UpdateDashboard();
        }

        #endregion

        #region Dashboard Logic
        public void UpdateDashboard()
        {
            double totalProfit = 0, totalRevenue = 0, totalCost = 0, totalROI = 0;
            double totalShipping = 0, totalFees = 0;
            int soldCount = 0, unsoldCount = 0;

            foreach (var item in inventory)
            {
                if (DateTime.TryParse(item.DatePurchased, out DateTime purchaseDate))
                {
                    if (startDateFilter.HasValue && endDateFilter.HasValue &&
                        (purchaseDate < startDateFilter || purchaseDate >= endDateFilter))
                    {
                        continue;
                    }

                    switch (item.LifecycleIndex)
                    {
                        case 1: // Sourced
                        case 2: // Listed
                        case 3: // Sold
                            double totalItemCost = item.PurchasePrice + item.Shipping + item.Fees;
                            double profit = item.SellingPrice - totalItemCost;

                            totalCost += item.PurchasePrice;
                            totalShipping += item.Shipping;
                            totalFees += item.Fees;
                            totalProfit += profit;
                            totalRevenue += item.SellingPrice;
                            totalROI += (profit / (item.PurchasePrice == 0 ? 1 : item.PurchasePrice)) * 100;
                            soldCount++;
                            break;
                        case 4: // Returned
                        case 5: // Pending
                            totalCost += item.PurchasePrice;
                            unsoldCount++;
                            break;
                    }
                }
            }

            double avgSalePrice = soldCount > 0 ? totalRevenue / soldCount : 0;
            double grossMargin = totalRevenue > 0 ? (totalProfit / totalRevenue) * 100 : 0;
            double roi = soldCount > 0 ? totalROI / soldCount : 0;
            double turnoverRate = (soldCount + unsoldCount > 0) ? (double)soldCount / (soldCount + unsoldCount) : 0;

            double expensesTotal = expense
                .Where(e => DateTime.TryParse(e.DatePurchased, out DateTime expenseDate) &&
                    (!startDateFilter.HasValue || expenseDate >= startDateFilter) &&
                    (!endDateFilter.HasValue || expenseDate < endDateFilter))
                .Sum(e => e.PurchasePrice);

            // Update UI
            SetTextBlock(RevenueTextBlock, $"Revenue: {totalProfit.ToString("C", CultureInfo.CurrentCulture)}");
            SetTextBlock(GrossRevenueTextBlock, $"Gross Revenue: {totalRevenue.ToString("C", CultureInfo.CurrentCulture)}");
            SetTextBlock(GrossMarginTextBlock, $"Gross Margin: {grossMargin:F2}%");
            SetTextBlock(TotalCostOfGoodsTextBlock, $"Total Cost Of Goods: {totalCost.ToString("C", CultureInfo.CurrentCulture)}");
            SetTextBlock(ReturnOnInventmentTextBlock, $"Return On Investment (ROI): {roi:F2}%");
            SetTextBlock(AverageSalePriceTextBlock, $"Average Sale Price: {avgSalePrice.ToString("C", CultureInfo.CurrentCulture)}");
            SetTextBlock(InventoryTurnoverRateTextBlock, $"Turnover Rate: {turnoverRate:F2}");
            SetTextBlock(ExpensesTextBlock, $"Expenses: {expensesTotal.ToString("C", CultureInfo.CurrentCulture)}");
            SetTextBlock(ShippingTextBlock, $"Shipping Costs: {totalShipping.ToString("C", CultureInfo.CurrentCulture)}");
            SetTextBlock(FeesTextBlock, $"Fees: {totalFees.ToString("C", CultureInfo.CurrentCulture)}");

            ReturnOnInventmentTextBlock.Foreground =
                roi < RoiThreshold - YellowLeeway ? Brushes.IndianRed :
                roi <= RoiThreshold + YellowLeeway ? Brushes.Goldenrod :
                Brushes.LightGreen;

            ApplyRowColoring();

            // Lifecycle stage summary
            var stageLabels = new Dictionary<int, string>
            {
                { 1, "Sourced" }, 
                { 2, "Listed" }, 
                { 3, "Sold" }, 
                { 4, "Returned" }, 
                { 5, "Pending" }
            };
            var stageCounts = DatabaseHelper.GetStageCounts();
            StringBuilder stageSummaryBuilder = new StringBuilder();
            foreach (var kvp in stageLabels)
            {
                int count = stageCounts.ContainsKey(kvp.Key) ? stageCounts[kvp.Key] : 0;
                stageSummaryBuilder.AppendLine($"{kvp.Value}: {count}");
            }
            SetTextBlock(StageSummaryTextBlock, stageSummaryBuilder.ToString());

            // === Graph Data Reset ===
            DashboardLabels.Clear();
            foreach (var series in DashboardSeries)
                series.Values.Clear();

            // === Group inventory data by day ===
            var groupedByDate = inventory
                .Where(i => DateTime.TryParse(i.DatePurchased, out DateTime parsedDate) &&
                           (!reportFilterStartDate.HasValue || parsedDate >= reportFilterStartDate) &&
                           (!reportFilterEndDate.HasValue || parsedDate <= reportFilterEndDate))
                .GroupBy(i => DateTime.Parse(i.DatePurchased).Date)
                .OrderBy(g => g.Key);
            foreach (var group in groupedByDate)
            {
                DateTime date = group.Key;
                DashboardLabels.Add(date.ToString("MMM dd"));

                double revenue = group.Where(i => i.LifecycleIndex == 3).Sum(i => i.SellingPrice);
                double cost = group.Sum(i => i.PurchasePrice);
                double profit = group.Where(i => i.LifecycleIndex == 3).Sum(i => i.SellingPrice - i.PurchasePrice);
                double shipping = group.Sum(i => i.Shipping);
                double fees = group.Sum(i => i.Fees);

                DashboardSeries[0].Values.Add(new DateTimePoint(date, Math.Round(revenue, 2)));
                DashboardSeries[1].Values.Add(new DateTimePoint(date, Math.Round(cost, 2)));
                DashboardSeries[2].Values.Add(new DateTimePoint(date, Math.Round(profit, 2)));
                DashboardSeries[3].Values.Add(new DateTimePoint(date, Math.Round(shipping, 2)));
                DashboardSeries[4].Values.Add(new DateTimePoint(date, Math.Round(fees, 2)));

                // ⚠️ Check for single data point case
                if (groupedByDate.Count() == 1)
                {
                    var extraDate = date.AddDays(1); // +1 day just for padding
                    DashboardLabels.Add(extraDate.ToString("MMM dd"));
                    DashboardSeries[0].Values.Add(new DateTimePoint(extraDate, 0));
                    DashboardSeries[1].Values.Add(new DateTimePoint(extraDate, 0));
                    DashboardSeries[2].Values.Add(new DateTimePoint(extraDate, 0));
                    DashboardSeries[3].Values.Add(new DateTimePoint(extraDate, 0));
                    DashboardSeries[4].Values.Add(new DateTimePoint(extraDate, 0));
                }
            }

            foreach (var series in DashboardSeries)
            {
                if (series is LineSeries lineSeries)
                {
                    lineSeries.Visibility = Visibility.Visible;
                    seriesVisibility[lineSeries.Title] = true;
                }
            }
        }

        private void SetTextBlock(TextBlock tb, string text)
        {
            if (tb != null)
                tb.Text = text;
        }
        private void ApplyRowColoring()
        {
            foreach (var inv in inventory)
            {
                if (inv.LifecycleIndex == 3 && inv.PurchasePrice > 0)
                {
                    double roi = ((inv.SellingPrice - inv.PurchasePrice) / inv.PurchasePrice) * 100;
                    double threshold = RoiThreshold;
                    double leeway = YellowLeeway;

                    if (roi < threshold - leeway)
                        inv.RowColor = Brushes.IndianRed;
                    else if (roi <= threshold + leeway)
                        inv.RowColor = Brushes.Goldenrod;
                    else
                        inv.RowColor = Brushes.LightGreen;
                }
                else
                {
                    inv.RowColor = Brushes.White;
                }
            }
        }

        #endregion

        #region Button Logic

        private void AddSaleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string category = CategoryComboBox.SelectedItem as string ?? "Uncategorized"; // Default to "Uncategorized" if no selection is made
                int marketplaceIndex = MarketplaceInputComboBox.SelectedIndex;
                int lifecycleIndex = LifecycleComboBox.SelectedIndex;
                int itemConditionIndex = ItemConditionComboBox.SelectedIndex;
                int shippingMethodIndex = ShippingMethodComboBox.SelectedIndex;

                if (!double.TryParse(PurchasePriceBox.Text, out double purchasePrice) ||
                    !double.TryParse(SellingPriceBox.Text, out double sellingPrice) ||
                    string.IsNullOrWhiteSpace(ItemNameBox.Text) ||
                    string.IsNullOrWhiteSpace(BrandNameBox.Text) ||
                    string.IsNullOrWhiteSpace(SkuBox.Text) ||
                    !DateBox.SelectedDate.HasValue ||
                    CategoryComboBox.SelectedIndex == 0 ||
                    MarketplaceInputComboBox.SelectedIndex == 0 ||
                    LifecycleComboBox.SelectedIndex == 0 ||
                    ItemConditionComboBox.SelectedIndex == 0 ||
                    ShippingMethodComboBox.SelectedIndex == 0)
                {
                    MessageBox.Show("Please enter valid item info.");
                    return;
                }

                var item = new InventoryItem
                {
                    Category = category,
                    MarketplaceIndex = marketplaceIndex,
                    ItemName = ItemNameBox.Text,
                    Brand = BrandNameBox.Text,
                    Description = "",
                    PurchasePrice = purchasePrice,
                    SellingPrice = sellingPrice,
                    SKU = SkuBox.Text,
                    DatePurchased = DateBox.SelectedDate.Value.ToString("MM/dd/yyyy"),
                    Tags = TagsBox.Text,
                    LifecycleIndex = lifecycleIndex,
                    ItemConditionIndex = itemConditionIndex,
                    ShippingMethodIndex = shippingMethodIndex,
                    ItemNotes = "",
                    AttachmentPaths = "",
                    Shipping = 0,
                    Fees = 0,
                };

                ApplyRowColoring();
                inventory.Add(item);
                DatabaseHelper.AddInventoryItem(item);
                UpdateDashboard();
                ClearInputFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Crash occurred:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}");
            }
        }


        private void AddExpenseButton_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(ExpensePurchasePriceBox.Text, out double purchasePrice) ||
                string.IsNullOrWhiteSpace(ExpenseItemNameBox.Text) ||
                !ExpenseDateBox.SelectedDate.HasValue)
            {
                MessageBox.Show("Please enter valid expense info.");
                return;
            }

            var expenseItem = new Expenses
            {
                ItemName = ExpenseItemNameBox.Text,
                PurchasePrice = purchasePrice,
                DatePurchased = ExpenseDateBox.SelectedDate.Value.ToString("MM/dd/yyyy")
            };

            expense.Add(expenseItem);
            DatabaseHelper.AddExpenseItem(expenseItem);
            UpdateDashboard();
            ClearInputFields();
        }

        private void ClearInputFields()
        {
            ItemNameBox.Text = "";
            PurchasePriceBox.Text = "";
            SellingPriceBox.Text = "";
            SkuBox.Text = "";
            DateBox.SelectedDate = null;
            TagsBox.Text = "";
            ExpenseItemNameBox.Text = "";
            ExpensePurchasePriceBox.Text = "";
            ExpenseDateBox.SelectedDate = null;

            // Reset category selection to the first item
            CategoryComboBox.SelectedIndex = 0;
            MarketplaceInputComboBox.SelectedIndex = 0;
            LifecycleComboBox.SelectedIndex = 0;
            ItemConditionComboBox.SelectedIndex = 0;
            ShippingMethodComboBox.SelectedIndex = 0;

            // Optionally set focus back to the first field
            ItemNameBox.Focus();
            ExpenseItemNameBox.Focus();
        }
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the SKU of the item to delete
                string skuToDelete = (string)((Button)sender).Tag;

                // Ask the user for confirmation
                var result = MessageBox.Show("Are you sure you want to delete this item?", "Confirm Deletion", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    // Call the database helper method to delete the item
                    DatabaseHelper.DeleteInventoryItem(skuToDelete);

                    // Get the current ObservableCollection bound to the DataGrid
                    var items = (ObservableCollection<InventoryItem>)InventoryTable.ItemsSource;

                    // Find the item to delete based on SKU
                    var itemToDelete = items.FirstOrDefault(item => item.SKU == skuToDelete);
                    if (itemToDelete != null)
                    {
                        // Remove the item from the ObservableCollection
                        items.Remove(itemToDelete);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error occurred: {ex.Message}");
            }
        }

        private void DeleteExpenseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the item name of the expense to delete
                string itemNameToDelete = (string)((Button)sender).Tag;

                // Ask the user for confirmation
                var result = MessageBox.Show("Are you sure you want to delete this expense?", "Confirm Deletion", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    // Call the database helper method to delete the expense
                    DatabaseHelper.DeleteExpenseItem(itemNameToDelete);

                    // Get the current ObservableCollection bound to the DataGrid
                    var expenses = (ObservableCollection<Expenses>)ExpenseTable.ItemsSource;

                    // Find the item to delete based on ItemName
                    var expenseToDelete = expenses.FirstOrDefault(exp => exp.ItemName == itemNameToDelete);
                    if (expenseToDelete != null)
                    {
                        // Remove the item from the ObservableCollection
                        expenses.Remove(expenseToDelete);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error occurred: {ex.Message}");
            }
        }

        private void DashboardFilterButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            string tag = button?.Tag?.ToString();

            DateTime now = DateTime.Now;
            switch (tag)
            {
                case "Today":
                    startDateFilter = now.Date;
                    endDateFilter = now.Date.AddDays(1);
                    break;
                case "Yesterday":
                    startDateFilter = now.Date.AddDays(-1);
                    endDateFilter = now.Date;
                    break;
                case "LastWeek":
                    startDateFilter = now.Date.AddDays(-7);
                    endDateFilter = now.Date.AddDays(1);
                    break;
                case "LastMonth":
                    startDateFilter = now.Date.AddMonths(-1);
                    endDateFilter = now.Date.AddDays(1);
                    break;
                case "Last3Months":
                    startDateFilter = now.Date.AddMonths(-3);
                    endDateFilter = now.Date.AddDays(1);
                    break;
                case "Last6Months":
                    startDateFilter = now.Date.AddMonths(-6);
                    endDateFilter = now.Date.AddDays(1);
                    break;
                case "AllTime":
                default:
                    startDateFilter = null;
                    endDateFilter = null;
                    break;
            }

            UpdateDashboard(); // Recalculate based on the new date range
        }

        private void InventoryFilterButton_Click(object sender, RoutedEventArgs e)
        {
            PopulateCategoryFilterComboBox();
            MarketplaceCategoryFilterComboBox.SelectedIndex = 0;
            LifecycleFilterComboBox.SelectedIndex = 0;
            FilterPopup.IsOpen = true;
        }

        private void ExpensesFilterButton_Click(object sender, RoutedEventArgs e)
        {
            ExpensesFilterPopup.IsOpen = true;
        }

        private void ApplyFilterButton_Click(object sender, RoutedEventArgs e)
        {
            string itemNameFilter = ItemNameFilterTextBox.Text?.Trim().ToLower();
            string itemTagFilter = TagsFilterTextBox.Text?.Trim().ToLower();
            double minPrice = 0, maxPrice = double.MaxValue;

            if (double.TryParse(MinPurchasePriceTextBox.Text, out var min))
                minPrice = min;

            if (double.TryParse(MaxPurchasePriceTextBox.Text, out var max))
                maxPrice = max;

            int selectedCategoryIndex = CategoryFilterComboBox.SelectedIndex;
            int selectedMarketplaceIndex = MarketplaceCategoryFilterComboBox.SelectedIndex;
            int selectedStageIndex = LifecycleFilterComboBox.SelectedIndex;

            var filteredItems = inventory.Where(item =>
                (string.IsNullOrEmpty(itemNameFilter) || item.ItemName.ToLower().Contains(itemNameFilter)) &&
                (selectedCategoryIndex <= 0 || item.Category == (string)CategoryFilterComboBox.SelectedItem) &&
                (selectedMarketplaceIndex <= 0 || item.MarketplaceIndex == selectedMarketplaceIndex) &&
                (selectedStageIndex <= 0 || item.LifecycleIndex == selectedStageIndex) &&
                (string.IsNullOrEmpty(itemTagFilter) || item.Tags.ToLower().Contains(itemTagFilter)) &&
                item.PurchasePrice >= minPrice && item.PurchasePrice <= maxPrice).ToList();

            InventoryTable.ItemsSource = filteredItems;

            FilterPopup.IsOpen = false;
        }

        private void ExpensesApplyFilterButton_Click(object sender, RoutedEventArgs e)
        {
            string itemNameFilter = ExpenseItemNameFilterTextBox.Text?.Trim().ToLower();
            double minPrice = 0, maxPrice = double.MaxValue;

            if (double.TryParse(ExpenseMinPurchasePriceTextBox.Text, out var min))
                minPrice = min;

            if (double.TryParse(ExpenseMaxPurchasePriceTextBox.Text, out var max))
                maxPrice = max;

            var filteredItems = expense.Where(item =>
                ((string.IsNullOrEmpty(itemNameFilter) || item.ItemName.ToLower().Contains(itemNameFilter)) &&
                item.PurchasePrice >= minPrice &&
                item.PurchasePrice <= maxPrice
            ));

            ExpenseTable.ItemsSource = filteredItems;

            ExpensesFilterPopup.IsOpen = false;
        }

        private void CloseFilterPopupButton_Click(object sender, RoutedEventArgs e)
        {
            FilterPopup.IsOpen = false;
            ExpensesFilterPopup.IsOpen = false;
        }

        private void ResetFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            ItemNameFilterTextBox.Text = string.Empty;
            CategoryFilterComboBox.SelectedIndex = 0;
            MarketplaceCategoryFilterComboBox.SelectedIndex = 0;
            LifecycleFilterComboBox.SelectedIndex = 0;
            MinPurchasePriceTextBox.Text = string.Empty;
            MaxPurchasePriceTextBox.Text = string.Empty;
            TagsFilterTextBox.Text = string.Empty;

            // Reset filter result
            inventory = new ObservableCollection<InventoryItem>(DatabaseHelper.LoadInventoryItems());

            // Ensure ItemsSource is updated correctly
            InventoryTable.ItemsSource = inventory;

            FilterPopup.IsOpen = false;
        }

        private void ExpensesResetFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear textboxes
            ExpenseItemNameFilterTextBox.Text = string.Empty;
            ExpenseMinPurchasePriceTextBox.Text = string.Empty;
            ExpenseMaxPurchasePriceTextBox.Text = string.Empty;

            // Reset filter result
            expense = new ObservableCollection<Expenses>(DatabaseHelper.LoadExpenseItems());

            // Refresh the binding
            ExpenseTable.ItemsSource = inventory;

            ExpensesFilterPopup.IsOpen = false;
        }
        private void PopulateCategoryFilterComboBox()
        {
            // Ensure Categories list doesn't contain the "Add new..." option when binding
            var filteredCategories = Categories.Take(Categories.Count - 1).ToList(); // Remove last element

            CategoryFilterComboBox.ItemsSource = filteredCategories;  // Bind ItemsSource

            CategoryFilterComboBox.SelectedIndex = 0;
        }
        private void QuickAddButton_Click(object sender, RoutedEventArgs e)
        {
            var quickAddWindow = new QuickAddWindow(inventory);
            quickAddWindow.Owner = this;
            quickAddWindow.ShowDialog();
        }
        private void AddFilesButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "Supported Files|*.jpg;*.jpeg;*.png;*.pdf;*.bmp;*.gif|All Files|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                if (InventoryTable.SelectedItem is InventoryItem selectedItem)
                {
                    string targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlipTrackr", "Attachments", selectedItem.Id.ToString());
                    Directory.CreateDirectory(targetDir);

                    var existingPaths = selectedItem.AttachmentPaths?.Split(';')
                        .Where(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p))
                        .ToList() ?? new List<string>();

                    foreach (string file in openFileDialog.FileNames)
                    {
                        string fileName = Path.GetFileName(file);
                        string destPath = Path.Combine(targetDir, fileName);
                        File.Copy(file, destPath, true);
                        existingPaths.Add(destPath);
                    }

                    // Ensure paths are unique and assigned
                    selectedItem.AttachmentPaths = string.Join(";", existingPaths.Distinct());
                    DatabaseHelper.UpdateInventoryItem(selectedItem);
                    MessageBox.Show("Files added successfully!");
                }
            }
        }
        private void ViewFilesButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is InventoryItem selectedItem)
            {
                var paths = selectedItem.AttachmentPaths?.Split(';').ToList() ?? new List<string>();

                var viewer = new AttachmentViewerWindow(paths, selectedItem);
                viewer.Owner = this;
                viewer.ShowDialog();
            }
        }
        private void ApplyReportDateFilter_Click(object sender, RoutedEventArgs e)
        {
            reportFilterStartDate = ReportStartDatePicker.SelectedDate;
            reportFilterEndDate = ReportEndDatePicker.SelectedDate?.AddDays(1); // Inclusive

            UpdateDashboard(); // This already drives the chart
        }

        private void ResetDateFilter_Click(object sender, RoutedEventArgs e)
        {
            ReportStartDatePicker.SelectedDate = null;
            ReportEndDatePicker.SelectedDate = null;
            reportFilterStartDate = null;
            reportFilterEndDate = null;

            UpdateDashboard();
        }

        private void LegendItem_Click(object sender, MouseButtonEventArgs e)
        {
            var textBlock = sender as TextBlock;
            var viewModel = textBlock?.DataContext as SeriesViewModel;

            if (viewModel != null)
            {
                viewModel.IsVisible = !viewModel.IsVisible;
                viewModel.Series.Visibility = viewModel.IsVisible ? Visibility.Visible : Visibility.Hidden;
                // No need to manually set Foreground — WPF will update it
            }
        }

        #region Menu Bar Buttons
        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(RoiThreshold);
            settingsWindow.Owner = this;

            if (settingsWindow.ShowDialog() == true)
            {
                RoiThreshold = settingsWindow.Threshold;
                ApplyRowColoring(); // re-evaluate with new threshold
            }
        }
        private void SaveAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in InventoryTable.Items.OfType<InventoryItem>())
                DatabaseHelper.UpdateInventoryItem(item);

            foreach (var exp in ExpenseTable.Items.OfType<Expenses>())
                DatabaseHelper.UpdateExpenseItem(exp);

            MessageBox.Show("All data saved successfully.", "Save All");
        }

        private void CreateBackup_Click(object sender, RoutedEventArgs e)
        {
            string dbPath = Path.Combine(AppDataPath, "fliptrackr.db");
            string backupDir = Path.Combine(AppDataPath, "Backups");
            Directory.CreateDirectory(backupDir);

            string backupFile = Path.Combine(backupDir, $"fliptrackr_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");

            try
            {
                File.Copy(dbPath, backupFile, true);
                MessageBox.Show($"Backup created:\n{backupFile}", "Backup Created");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating backup:\n{ex.Message}", "Error");
            }
        }

        private void RestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select a backup file",
                Filter = "SQLite Database (*.db)|*.db",
                InitialDirectory = Path.Combine(AppDataPath, "Backups")
            };

            if (dialog.ShowDialog() == true)
            {
                string dbPath = Path.Combine(AppDataPath, "fliptrackr.db");

                try
                {
                    File.Copy(dialog.FileName, dbPath, true);
                    MessageBox.Show("Backup restored.\nPlease restart the application.", "Restore Complete");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error restoring backup:\n{ex.Message}", "Error");
                }
            }
        }
        private void ExportInventoryToCsv_Click(object sender, RoutedEventArgs e)
        {
            string dbPath = Path.Combine(AppDataPath, "fliptrackr.db");
            string exportDir = Path.Combine(AppDataPath, "Exports");
            Directory.CreateDirectory(exportDir);

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export Inventory to CSV",
                Filter = "CSV files (*.csv)|*.csv",
                FileName = $"inventory_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    using (var writer = new StreamWriter(saveDialog.FileName))
                    {
                        // Header row
                        writer.WriteLine("Item Name,Brand,Category,Description,Purchase Price,Selling Price,Date Purchased,SKU,Marketplace,Stage,Item Condition,Shipping Method,Shipping,Fees,Tags,Notes");

                        foreach (var item in inventory)
                        {
                            string line = string.Join(",",
                                EscapeForCsv(item.ItemName),
                                EscapeForCsv(item.Brand),
                                EscapeForCsv(item.Category),
                                EscapeForCsv(item.Description),
                                item.PurchasePrice.ToString("F2", CultureInfo.InvariantCulture),
                                item.SellingPrice.ToString("F2", CultureInfo.InvariantCulture),
                                EscapeForCsv(item.DatePurchased),
                                EscapeForCsv(item.SKU),
                                EscapeForCsv(item.MarketplaceName),
                                EscapeForCsv(item.LifecycleStage),
                                EscapeForCsv(item.ItemConditionStage),
                                EscapeForCsv(item.ShippingMethodStage),
                                item.Shipping.ToString("F2", CultureInfo.InvariantCulture),
                                item.Fees.ToString("F2", CultureInfo.InvariantCulture),
                                EscapeForCsv(item.Tags),
                                EscapeForCsv(item.ItemNotes));

                            writer.WriteLine(line);
                        }
                    }

                    MessageBox.Show("Inventory exported successfully.", "Export Complete");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting inventory:\n{ex.Message}", "Error");
                }
            }
        }

        // Helper to escape commas/quotes
        private string EscapeForCsv(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            if (input.Contains(",") || input.Contains("\""))
                return $"\"{input.Replace("\"", "\"\"")}\"";
            return input;
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        private void ReportBug_Click(object sender, RoutedEventArgs e)
        {
            var bugWindow = new ReportBugWindow();
            bugWindow.Owner = this;
            bugWindow.ShowDialog();
        }

        private void Instruction_Click(object sender, RoutedEventArgs e)
        {
            var instructionWindow = new InstructionWindow();
            instructionWindow.Owner = this;
            instructionWindow.ShowDialog();
        }

        private void TermsOfService_Click(object sender, RoutedEventArgs e)
        {
            var termsWindow = new TermsOfServiceWindow();
            termsWindow.Owner = this;
            termsWindow.ShowDialog();
        }

        #endregion

        #endregion

        #region Revenue Calculator Logic
        private void CalculatorField_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;

            if (textBox == null)
                return;

            string input = textBox.Text;

            // Allow only digits and a single decimal point
            int dotCount = input.Count(c => c == '.');

            if (!double.TryParse(input, out _) || dotCount > 1)
            {
                // Keep only the first valid decimal point
                bool decimalFound = false;
                var corrected = new StringBuilder();
                foreach (char c in input)
                {
                    if (char.IsDigit(c))
                    {
                        corrected.Append(c);
                    }
                    else if (c == '.' && !decimalFound)
                    {
                        corrected.Append(c);
                        decimalFound = true;
                    }
                }

                textBox.Text = corrected.ToString();
                textBox.CaretIndex = textBox.Text.Length;
            }

            RevenueCalculator(); // Now safe to calculate
        }

        public void RevenueCalculator()
        {
            bool isCostValid = double.TryParse(ItemCostTextBox.Text, out double cost);
            bool isSellingPriceValid = double.TryParse(SellingPriceTextBox.Text, out double sellingPrice);

            if (isCostValid && isSellingPriceValid)
            {
                double calcTotalProfit = sellingPrice - cost;
                double calcTotalRevenue = sellingPrice;

                double calcGrossMargin = (calcTotalRevenue > 0) ? (calcTotalProfit / calcTotalRevenue) * 100 : 0;

                SetTextBlock(CalcRevenueTextBlock, $"Revenue: {calcTotalProfit.ToString("C", CultureInfo.CurrentCulture)}");
                SetTextBlock(CalcGrossRevenueTextBlock, $"Gross Revenue: {calcTotalRevenue.ToString("C", CultureInfo.CurrentCulture)}");
                SetTextBlock(CalcGrossMarginTextBlock, $"Gross Margin: {calcGrossMargin:F2}%");
            }
            else
            {
                // If one or both fields are empty, show blanks but NO crash
                SetTextBlock(CalcRevenueTextBlock, $"Revenue: {0.0.ToString("C", CultureInfo.CurrentCulture)}");
                SetTextBlock(CalcGrossRevenueTextBlock, $"Gross Revenue: {0.0.ToString("C", CultureInfo.CurrentCulture)}");
                SetTextBlock(CalcGrossMarginTextBlock, "Gross Margin: 0%");
            }
        }

        #endregion
    }
}