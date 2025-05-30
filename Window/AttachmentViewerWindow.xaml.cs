using FlipTrackr.Handlers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Runtime.CompilerServices;
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
    public partial class AttachmentViewerWindow : Window
    {
        private readonly InventoryItem selectedItem;
        private readonly ObservableCollection<AttachmentViewModel> attachments;

        public AttachmentViewerWindow(List<string> filePaths, InventoryItem item)
        {
            InitializeComponent();
            selectedItem = item;

            attachments = new ObservableCollection<AttachmentViewModel>(
                filePaths
                    .Where(File.Exists)
                    .Select(path => new AttachmentViewModel { File = new FileInfo(path) })
            );

            AttachmentsPanel.ItemsSource = attachments;
        }
        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string filePath && File.Exists(filePath))
            {
                try
                {
                    string extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();

                    if (extension == ".pdf")
                    {
                        // Open with the default browser (specifically tells Windows to use http handler)
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "cmd",
                            Arguments = $"/c start \"\" \"{filePath}\"",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        });
                    }
                    else
                    {
                        // Open other file types normally
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath)
                        {
                            UseShellExecute = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string filePath && File.Exists(filePath))
            {
                try
                {
                    // Remove any references to the file to unlock it
                    var container = AttachmentsPanel.ItemContainerGenerator.ContainerFromItem(
                        attachments.FirstOrDefault(f => f.FullName == filePath)) as FrameworkElement;

                    if (container != null)
                    {
                        var image = FindVisualChild<Image>(container);
                        if (image != null)
                            image.Source = null; // Release the file lock
                    }

                    File.Delete(filePath);

                    // Remove from UI
                    var target = attachments.FirstOrDefault(f => string.Equals(f.FullName, filePath, StringComparison.OrdinalIgnoreCase));
                    if (target != null)
                    {
                        attachments.Remove(target);
                    }

                    // Update database
                    string updatedPaths = string.Join(";", attachments.Select(f => f.FullName));
                    selectedItem.AttachmentPaths = updatedPaths;
                    DatabaseHelper.UpdateInventoryItem(selectedItem);

                    MessageBox.Show("File deleted.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T correctlyTyped)
                    return correctlyTyped;

                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                    return descendant;
            }
            return null;
        }
    }
}
