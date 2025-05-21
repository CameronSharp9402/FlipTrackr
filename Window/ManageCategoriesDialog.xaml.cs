using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
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
    public partial class ManageCategoriesDialog : Window
    {
        public ObservableCollection<string> Categories { get; }

        public ManageCategoriesDialog(ObservableCollection<string> categories)
        {
            InitializeComponent();

            // Exclude the first and last (Select Category, Add new...)
            Categories = new ObservableCollection<string>(categories.Skip(1).Take(categories.Count - 2));
            CategoryList.ItemsSource = Categories;
        }

        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            string selectedCategory = CategoryList.SelectedItem as string;

            if (selectedCategory != null && selectedCategory != "Add new..." && selectedCategory != "Select Category")
            {
                // Check if the category is used by any inventory item
                var inventoryItems = DatabaseHelper.LoadInventoryItems();
                bool isCategoryInUse = inventoryItems.Any(item => item.Category?.Trim() == selectedCategory.Trim());

                if (isCategoryInUse)
                {
                    MessageBox.Show("This category is currently used by one or more inventory items and cannot be deleted.");
                    return;
                }

                // Confirm with the user before deleting
                var result = MessageBox.Show($"Are you sure you want to delete the category '{selectedCategory}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    // Delete category from the database
                    DatabaseHelper.DeleteCategory(selectedCategory);

                    // Refresh the Categories collection (reload after deletion)
                    var updatedCategories = DatabaseHelper.LoadCategories(); // Reload the categories
                    Categories.Clear();
                    foreach (var cat in updatedCategories.Skip(1).Take(updatedCategories.Count - 2))
                    {
                        Categories.Add(cat);
                    }

                    // Ensure the dialog closes after deletion
                    this.DialogResult = true;  // Marks the dialog as successful and closes it
                    this.Close();              // Closes the dialog
                }
            }
        }
    }
}
