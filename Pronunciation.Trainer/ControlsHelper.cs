using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;

namespace Pronunciation.Trainer
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
    }
}
