using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace FlipTrackr.Handlers
{
    public class AttachmentViewModel
    {
        public FileInfo File { get; set; }

        public string Name => File.Name;
        public string FullName => File.FullName;

        public BitmapImage? PreviewImage
        {
            get
            {
                try
                {
                    string ext = File.Extension.ToLower();
                    if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".gif" || ext == ".bmp")
                    {
                        using var fs = new FileStream(FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = fs;
                        image.EndInit();
                        image.Freeze(); // Make it cross-thread safe
                        return image;
                    }
                }
                catch
                {
                    // Fail silently; fallback will handle this.
                }
                return null;
            }
        }

        public Visibility ImageVisibility => PreviewImage != null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility FallbackVisibility => PreviewImage == null ? Visibility.Visible : Visibility.Collapsed;
    }
}
