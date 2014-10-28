using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.IO;
using System.Reflection;

namespace Pronunciation.Trainer.Utility
{
    public class ControlsHelper
    {
        private enum AllowedContainerType
        {
            Window,
            UserControl,
            Both
        }

        // Returns either containing UserControl or Window
        public static ContentControl GetContainer(DependencyObject element)
        {
            return GetContainer(element, AllowedContainerType.Both);
        }

        private static ContentControl GetContainer(DependencyObject element, AllowedContainerType containerType)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(element);
            if (parent == null)
                return null;

            if ((parent is UserControl) && (containerType == AllowedContainerType.UserControl || containerType == AllowedContainerType.Both))
                return (UserControl)parent;

            if ((parent is Window) && (containerType == AllowedContainerType.Window || containerType == AllowedContainerType.Both))
                return (Window)parent;

            return GetContainer(parent);
        }

        public static Window GetWindow(DependencyObject control)
        {
            return Window.GetWindow(control);
        }

        public static bool IsInVisualTree(DependencyObject element)
        {
            return GetContainer(element, AllowedContainerType.Window) != null;
        }

        public static bool HasTextBecomeLonger(TextChangedEventArgs e)
        {
            if (e != null && e.Changes != null)
            {
                foreach (TextChange change in e.Changes)
                {
                    if (change.AddedLength > change.RemovedLength)
                        return true;
                }
            }

            return false;
        }

        public static bool IsExplicitCloseRequired(Button cancelButton)
        {
            // When window is shown with ShowDialog and there's a button with IsCancel = true we don't need
            // to call the Close explicitly (otherwise it will be called two times)
            return !(cancelButton.IsCancel && IsModalWindow(GetWindow(cancelButton))); 
        }

        public static bool IsModalWindow(Window window)
        {
            Type type = typeof(Window);
            var field = type.GetField("_showingAsDialog", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                return System.Windows.Interop.ComponentDispatcher.IsThreadModal;
            }
            else
            {
                return (bool)field.GetValue(window);
            }
        }

        public static BitmapImage ImageFromRawData(byte[] data)
        {
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            //image.UriSource = imageUrl;
            //image.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // required if we want to reload image from URL
            using (var imageStream = new MemoryStream(data))
            {
                image.StreamSource = imageStream;
                image.EndInit();
            }

            return image;
        }

        public static BitmapImage ImageFromUrl(Uri imageUrl)
        {
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            // Required if we want to reload image from URL
            image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            image.UriSource = imageUrl;
            image.EndInit();

            return image;
        }
    }
}
