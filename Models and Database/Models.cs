using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows;

namespace CSharpResaleBusinessTracker
{
    public class InventoryItem : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public bool SaleBreakdownShown { get; set; } = false;
        private bool isSold;
        private string category;
        private string itemName;
        private double purchasePrice;
        private double sellingPrice;
        private string datePurchased;
        private string sku;
        private string tags;
        private int marketplaceIndex;
        private int lifecycleIndex;
        private string itemNotes;
        private string attachmentPaths;
        private double shipping;
        private double fees;
        private Brush rowColor = Brushes.White;
        private string description;
        private int itemConditionIndex;
        private int shippingMethodIndex;
        private string brand;

        public string Brand
        {
            get => brand;
            set
            {
                if (brand != value)
                {
                    brand = value;
                    OnPropertyChanged(nameof(Brand));
                }
            }
        }

        public string ShippingMethodStage => ShippingMethodStages[ShippingMethodIndex];
        public int ShippingMethodIndex
        {
            get => shippingMethodIndex;
            set
            {
                if (shippingMethodIndex != value)
                {
                    shippingMethodIndex = value;
                    OnPropertyChanged(nameof(ShippingMethodIndex));
                    OnPropertyChanged(nameof(ShippingMethodStages));
                }
            }
        }

        public static readonly List<string> ShippingMethodStages = new List<string>
        {
        "Buyer - USPS",
        "Buyer - UPS",
        "Buyer - FedEx",
        "Seller - USPS",
        "Seller - UPS",
        "Seller - FedEx"
        };

        public string ItemConditionStage => ItemConditionStages[ItemConditionIndex];

        public int ItemConditionIndex
        {
            get => itemConditionIndex;
            set
            {
                if (itemConditionIndex != value)
                {
                    itemConditionIndex = value;
                    OnPropertyChanged(nameof(ItemConditionIndex));
                    OnPropertyChanged(nameof(ItemConditionStages));
                }
            }
        }

        public static readonly List<string> ItemConditionStages = new List<string>
        {
        "New",
        "Like New",
        "Good",
        "Fair",
        "Poor"
        };

        public string Description
        {
            get => description;
            set
            {
                if (description != value)
                {
                    description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        public double Fees
        {
            get => fees;
            set
            {
                if (fees != value)
                {
                    fees = value;
                    OnPropertyChanged(nameof(Fees));
                }
            }
        }

        public double Shipping
        {
            get => shipping;
            set
            {
                if (shipping != value)
                {
                    shipping = value;
                    OnPropertyChanged(nameof(Shipping));
                }
            }
        }
        public BitmapImage ThumbnailImage
        {
            get
            {
                try
                {
                    if (!string.IsNullOrEmpty(AttachmentPaths))
                    {
                        string firstFile = AttachmentPaths.Split(';').FirstOrDefault(); // Assuming semicolon-delimited
                        if (IsImageFile(firstFile) && File.Exists(firstFile))
                        {
                            return new BitmapImage(new Uri(firstFile));
                        }
                    }
                }
                catch { }

                return null;
            }
        }
        public Visibility IsNotImageVisible
        {
            get
            {
                if (string.IsNullOrEmpty(AttachmentPaths)) return Visibility.Visible;

                string firstFile = AttachmentPaths.Split(';').FirstOrDefault();
                return IsImageFile(firstFile) ? Visibility.Collapsed : Visibility.Visible;
            }
        }
        private bool IsImageFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;

            var ext = Path.GetExtension(path)?.ToLower();
            return ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif";
        }

        public string AttachmentPaths
        {
            get => attachmentPaths;
            set
            {
                if (attachmentPaths != value)
                {
                    attachmentPaths = value;
                    OnPropertyChanged(nameof(AttachmentPaths));
                }
            }
        }

        public string Category
        {
            get => category;
            set
            {
                if (category != value)
                {
                    category = value;
                    OnPropertyChanged(nameof(Category));
                }
            }
        }

        public string ItemName
        {
            get => itemName;
            set
            {
                if (itemName != value)
                {
                    itemName = value;
                    OnPropertyChanged(nameof(ItemName));
                }
            }
        }

        public double PurchasePrice
        {
            get => purchasePrice;
            set
            {
                if (purchasePrice != value)
                {
                    purchasePrice = value;
                    OnPropertyChanged(nameof(PurchasePrice));
                }
            }
        }

        public double SellingPrice
        {
            get => sellingPrice;
            set
            {
                if (sellingPrice != value)
                {
                    sellingPrice = value;
                    OnPropertyChanged(nameof(SellingPrice));
                }
            }
        }

        public string DatePurchased
        {
            get => datePurchased;
            set
            {
                if (datePurchased != value)
                {
                    datePurchased = value;
                    OnPropertyChanged(nameof(DatePurchased));
                }
            }
        }

        public string SKU
        {
            get => sku;
            set
            {
                if (sku != value)
                {
                    sku = value;
                    OnPropertyChanged(nameof(SKU));
                }
            }
        }

        public string Tags
        {
            get => tags;
            set
            {
                if (tags != value)
                {
                    tags = value;
                    OnPropertyChanged(nameof(Tags));
                }
            }
        }

        public string MarketplaceName => MarketplaceOptions[MarketplaceIndex];

        public int MarketplaceIndex
        {
            get => marketplaceIndex;
            set
            {
                if (marketplaceIndex != value)
                {
                    marketplaceIndex = value;
                    OnPropertyChanged(nameof(MarketplaceIndex));
                    OnPropertyChanged(nameof(MarketplaceName)); // in case bound elsewhere
                }
            }
        }

        public static readonly List<string> MarketplaceOptions = new List<string>
        {
        "Amazon", "eBay", "Mercari", "Facebook Marketplace",
        "OfferUp", "Craigslist", "Etsy", "Poshmark", "Depop", "Whatnot", "Other"
        };

        public string LifecycleStage => LifecycleStages[LifecycleIndex];

        public int LifecycleIndex
        {
            get => lifecycleIndex;
            set
            {
                if (lifecycleIndex != value)
                {
                    lifecycleIndex = value;
                    OnPropertyChanged(nameof(LifecycleIndex));
                    OnPropertyChanged(nameof(LifecycleStages));
                }
            }
        }

        public static readonly List<string> LifecycleStages = new List<string>
        {
        "Sourced",
        "Listed",
        "Sold",
        "Returned",
        "Pending"
        };

        public string ItemNotes
        {
            get => itemNotes;
            set
            {
                if (itemNotes != value)
                {
                    itemNotes = value;
                    OnPropertyChanged(nameof(ItemNotes));
                }
            }
        }

        public bool IsSold
        {
            get => isSold;
            set
            {
                if (isSold != value)
                {
                    isSold = value;
                    OnPropertyChanged(nameof(IsSold));
                }
            }
        }

        public Brush RowColor
        {
            get => rowColor;
            set
            {
                if (rowColor != value)
                {
                    rowColor = value;
                    OnPropertyChanged(nameof(RowColor));
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class Expenses : INotifyPropertyChanged
    {
        public int Id { get; set; }
        private string itemName;
        private double purchasePrice;
        private string datePurchase;
        public string ItemName
        {
            get => itemName;
            set
            {
                if (itemName != value) 
                    {
                        itemName = value;
                        OnPropertyChanged(nameof(ItemName));
                    }
            }
        }
        public double PurchasePrice
        {
            get => purchasePrice;
            set
            {
                if (purchasePrice != value)
                {
                    purchasePrice = value;
                    OnPropertyChanged(nameof(PurchasePrice));
                }
            }
        }
        public string DatePurchased
        {
            get => datePurchase;
            set
            {
                if (datePurchase != value)
                {
                    datePurchase = value;
                    OnPropertyChanged(nameof(DatePurchased));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}