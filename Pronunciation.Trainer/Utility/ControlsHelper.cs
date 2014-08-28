using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;

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
            return !(cancelButton.IsCancel && IsModalWindow); 
        }

        public static bool IsModalWindow
        {
            get { return System.Windows.Interop.ComponentDispatcher.IsThreadModal; }
        }
    }
}
