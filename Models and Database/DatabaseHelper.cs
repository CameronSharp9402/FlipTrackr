using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

namespace CSharpResaleBusinessTracker
{
    public static class DatabaseHelper
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FlipTrackr"
        );

        private static readonly string DbFileFullPath = Path.Combine(AppDataFolder, "FlipTrackr.db");
        private static readonly string DbConnectionString = $"Data Source={DbFileFullPath}";

        static DatabaseHelper()
        {
            EnsureDatabaseExists();
        }

        public static void EnsureDatabaseExists()
        {
            if (!Directory.Exists(AppDataFolder))
            {
                Directory.CreateDirectory(AppDataFolder);
            }

            if (!File.Exists(DbFileFullPath))
            {
                InitializeDatabase();
            }
        }

        public static void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(DbConnectionString))
            {
                connection.Open();

                var createInventoryTableCmd = connection.CreateCommand();
                createInventoryTableCmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS Inventory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ItemName TEXT,
                Brand TEXT,
                Category TEXT,
                Marketplace INTEGER,
                Description TEXT,
                PurchasePrice REAL,
                SellingPrice REAL,
                DatePurchased TEXT,
                SKU TEXT,
                IsSold INTEGER,
                Tags TEXT,
                Stage INTEGER,
                ItemCondition INTEGER,
                ShippingMethod INTEGER,
                ItemNotes TEXT,
                AttachmentPaths TEXT,
                Shipping INTEGER,
                Fees INTEGER
            );
            ";
                createInventoryTableCmd.ExecuteNonQuery();

                var createExpensesTableCmd = connection.CreateCommand();
                createExpensesTableCmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS Expenses (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ItemName TEXT,
                PurchasePrice REAL,
                DatePurchased TEXT
            );
            ";
                createExpensesTableCmd.ExecuteNonQuery();

                var createCategoriesTableCmd = connection.CreateCommand();
                createCategoriesTableCmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS Categories (
                Name TEXT PRIMARY KEY
            );
            ";
                createCategoriesTableCmd.ExecuteNonQuery();
            }
        }

        public static SqliteConnection GetConnection()
        {
            return new SqliteConnection(DbConnectionString);
        }

        public static Dictionary<int, int> GetStageCounts()
        {
            var counts = new Dictionary<int, int>();

            using (var connection = new SqliteConnection(DbConnectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT Stage, COUNT(*) FROM Inventory GROUP BY Stage";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int stage = reader.GetInt32(0);
                        int count = reader.GetInt32(1);
                        counts[stage] = count;
                    }
                }
            }

            return counts;
        }

        public static bool IsCategoryInUse(string category)
        {
            using (var connection = new SqliteConnection(DbConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM Inventory WHERE Category = $cat";
                command.Parameters.AddWithValue("$cat", category);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }
        public static void AddCategory(string name)
        {
            using (var connection = new SqliteConnection(DbConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "INSERT OR IGNORE INTO Categories (Name) VALUES ($name);";
                command.Parameters.AddWithValue("$name", name);
                command.ExecuteNonQuery();
            }
        }
        public static List<string> LoadCategories()
        {
            var categories = new List<string>();
            using (var connection = new SqliteConnection(DbConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Name FROM Categories ORDER BY Name;";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        categories.Add(reader.GetString(0));
                    }
                }
            }
            return categories;
        }

        public static void DeleteCategory(string category)
        {
            using (var connection = new SqliteConnection(DbConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Categories WHERE Name = $name";
                command.Parameters.AddWithValue("$name", category);
                command.ExecuteNonQuery();
            }
        }


        public static void AddInventoryItem(InventoryItem item)
        {
            using (var connection = new SqliteConnection(DbConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                @"
                INSERT INTO Inventory (ItemName, Brand, Category, Marketplace, Description, PurchasePrice, SellingPrice, DatePurchased, SKU, IsSold, Tags, Stage, ItemCondition, ShippingMethod, ItemNotes, AttachmentPaths, Shipping, Fees)
                VALUES ($itemname, $brand, $category, $marketplaceIndex, $description, $purchasePrice, $sellingPrice, $datePurchased, $sku, $isSold, $tags, $lifecycleIndex, $itemConditionIndex, $shippingMethodIndex, $itemNotes, $attachmentPaths, $shipping, $fees);
                ";
                command.Parameters.AddWithValue("$itemname", item.ItemName);
                command.Parameters.AddWithValue("$brand", item.Brand);
                command.Parameters.AddWithValue("$category", item.Category ?? "Unknown");
                command.Parameters.AddWithValue("$marketplaceIndex", item.MarketplaceIndex);
                command.Parameters.AddWithValue("$description", item.Description);
                command.Parameters.AddWithValue("$purchasePrice", item.PurchasePrice);
                command.Parameters.AddWithValue("$sellingPrice", item.SellingPrice);
                command.Parameters.AddWithValue("$datePurchased", item.DatePurchased);
                command.Parameters.AddWithValue("$sku", item.SKU);
                command.Parameters.AddWithValue("$isSold", item.IsSold ? 1 : 0);
                command.Parameters.AddWithValue("$tags", item.Tags);
                command.Parameters.AddWithValue("$lifecycleIndex", item.LifecycleIndex);
                command.Parameters.AddWithValue("$itemConditionIndex", item.ItemConditionIndex);
                command.Parameters.AddWithValue("$shippingMethodIndex", item.ShippingMethodIndex);
                command.Parameters.AddWithValue("$itemNotes", item.ItemNotes ?? "");
                command.Parameters.AddWithValue("$attachmentPaths", item.AttachmentPaths);
                command.Parameters.AddWithValue("$shipping", item.Shipping);
                command.Parameters.AddWithValue("$fees", item.Fees);
                command.ExecuteNonQuery();
            }
        }

        public static List<InventoryItem> LoadInventoryItems()
        {
            var items = new List<InventoryItem>();
            using (var connection = new SqliteConnection(DbConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Id, ItemName, Brand, Category, Marketplace, Description, PurchasePrice, SellingPrice, DatePurchased, SKU, IsSold, Tags, Stage, ItemCondition, ShippingMethod, ItemNotes, AttachmentPaths, Shipping, Fees FROM Inventory;";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                          items.Add(new InventoryItem
                          {
                              Id = reader.GetInt32(0),  // Read the Id field
                              ItemName = reader.GetString(1),
                              Brand = reader.GetString(2),
                              Category = reader.IsDBNull(3) ? null : reader.GetString(3),
                              MarketplaceIndex = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                              Description = reader.GetString(5),
                              PurchasePrice = reader.GetDouble(6),
                              SellingPrice = reader.GetDouble(7),
                              DatePurchased = reader.GetString(8),
                              SKU = reader.GetString(9),
                              IsSold = reader.GetInt32(10) == 1,
                              Tags = reader.GetString(11),
                              LifecycleIndex = reader.IsDBNull(12) ? 0 : reader.GetInt32(12),
                              ItemConditionIndex = reader.IsDBNull(13) ? 0 : reader.GetInt32(13),
                              ShippingMethodIndex = reader.IsDBNull(14) ? 0 : reader.GetInt32(14),
                              ItemNotes = reader.GetString(15),
                              AttachmentPaths = reader.IsDBNull(16) ? null : reader.GetString(16),
                              Shipping = reader.GetDouble(17),
                              Fees = reader.GetDouble(18),
                          });
                    }
                }
            }
            return items;
        }

        public static void AddExpenseItem(Expenses item)
        {
            using (var connection = new SqliteConnection(DbConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                @"
                INSERT INTO Expenses (ItemName, PurchasePrice, DatePurchased)
                VALUES ($itemname, $purchaseprice, $datepurchased);
                ";
                command.Parameters.AddWithValue("$itemname", item.ItemName);
                command.Parameters.AddWithValue("$purchaseprice", item.PurchasePrice);
                command.Parameters.AddWithValue("$datepurchased", item.DatePurchased);
                command.ExecuteNonQuery();
            }
        }

        public static List<Expenses> LoadExpenseItems()
        {
            var expenses = new List<Expenses>();
            using (var connection = new SqliteConnection(DbConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Id, ItemName, PurchasePrice, DatePurchased FROM Expenses;";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        expenses.Add(new Expenses
                        {
                            Id = reader.GetInt32(0),
                            ItemName = reader.GetString(1),
                            PurchasePrice = reader.GetDouble(2),
                            DatePurchased = reader.GetString(3)
                        });
                    }
                }
            }
            return expenses;
        }
        public static void DeleteInventoryItem(string sku)
        {
            using (var connection = new SqliteConnection(DbConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = 
                @"
                    DELETE FROM Inventory
                    WHERE SKU = $sku;
                ";
                command.Parameters.AddWithValue("$sku", sku);
                command.ExecuteNonQuery();
            }
        }

        public static void DeleteExpenseItem(string itemName)
        {
            using (var connection = new SqliteConnection(DbConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Expenses WHERE ItemName = $itemName;";
                command.Parameters.AddWithValue("$itemName", itemName);
                command.ExecuteNonQuery();
            }
        }

        public static void UpdateInventoryItem(InventoryItem item)
        {
            using (var connection = new SqliteConnection(DbConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                @"
                UPDATE Inventory
                SET 
                    ItemName = $itemname, 
                    Brand = $brand,
                    Category = $category, 
                    Marketplace = $marketplaceIndex,
                    Description = $description,
                    PurchasePrice = $purchasePrice, 
                    SellingPrice = $sellingPrice, 
                    DatePurchased = $datePurchased, 
                    SKU = $sku, 
                    IsSold = $isSold,
                    Tags = $tags,
                    Stage = $lifecycleIndex,
                    ItemCondition = $itemConditionIndex,
                    ShippingMethod = $shippingMethodIndex,
                    ItemNotes = $itemNotes,
                    AttachmentPaths = $attachmentPaths,
                    Shipping = $shipping,
                    Fees = $fees
                WHERE Id = $id;
                ";
                command.Parameters.AddWithValue("$itemname", item.ItemName);
                command.Parameters.AddWithValue("$brand", item.Brand);
                command.Parameters.AddWithValue("$category", item.Category);
                command.Parameters.AddWithValue("$marketplaceIndex", item.MarketplaceIndex);
                command.Parameters.AddWithValue("$description", item.Description);
                command.Parameters.AddWithValue("$purchasePrice", item.PurchasePrice);
                command.Parameters.AddWithValue("$sellingPrice", item.SellingPrice);
                command.Parameters.AddWithValue("$datePurchased", item.DatePurchased);
                command.Parameters.AddWithValue("$sku", item.SKU);
                command.Parameters.AddWithValue("$isSold", item.IsSold ? 1 : 0);
                command.Parameters.AddWithValue("$tags", item.Tags ?? string.Empty);
                command.Parameters.AddWithValue("$lifecycleIndex", item.LifecycleIndex);
                command.Parameters.AddWithValue("$itemConditionIndex", item.ItemConditionIndex);
                command.Parameters.AddWithValue("$shippingMethodIndex", item.ShippingMethodIndex);
                command.Parameters.AddWithValue("$itemNotes", item.ItemNotes);
                command.Parameters.AddWithValue("$attachmentPaths", item.AttachmentPaths ?? string.Empty);
                command.Parameters.AddWithValue("$shipping", item.Shipping);
                command.Parameters.AddWithValue("$fees", item.Fees);
                command.Parameters.AddWithValue("$id", item.Id);
                command.ExecuteNonQuery();
            }
        }

        public static void UpdateExpenseItem(Expenses item)
        {
            using (var connection = new SqliteConnection(DbConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                @"
                UPDATE Expenses
                SET 
                    ItemName = $itemname, 
                    PurchasePrice = $purchasePrice,  
                    DatePurchased = $datePurchased
                WHERE Id = $id;
                ";
                command.Parameters.AddWithValue("$itemname", item.ItemName);
                command.Parameters.AddWithValue("$purchasePrice", item.PurchasePrice);
                command.Parameters.AddWithValue("$datePurchased", item.DatePurchased);
                command.Parameters.AddWithValue("$id", item.Id);
                command.ExecuteNonQuery();
            }
        }
    }
}