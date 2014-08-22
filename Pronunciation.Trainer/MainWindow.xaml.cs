using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.ComponentModel;
using Pronunciation.Trainer.Utility;
using System.Reflection;
using Pronunciation.Trainer.Controls;
//using System.Windows.Shapes;

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent(); 
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            var activeTab = tabsRoot.SelectedItem as TabItem;
            if (activeTab != null)
            {
                // Get focus out of the current control (e.g. from textbox) to force its value to be flushed to the databinding
                activeTab.Focus();
            }
            AppSettings.Instance.Save();
        }

        //private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        //{
        //    var focus = FocusManager.GetFocusedElement(this);
        //    System.IO.File.AppendAllText(@"d:\focus.txt", 
        //        "F: " + (focus == null ? "null" : focus.ToString()) +
        //        " K: " + (Keyboard.FocusedElement == null ? "null" : Keyboard.FocusedElement.ToString()) + Environment.NewLine );
        //}

        private void Window_Activated(object sender, EventArgs e)
        {
            TabItemExt activeTab = tabsRoot.SelectedItem as TabItemExt;
            if (activeTab != null)
            {
                activeTab.CaptureKeyboardFocus();
            }
        }

        private void tabsRoot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var activeTab = tabsRoot.SelectedItem as TabItemExt;
            if (activeTab != null && activeTab.DynamicContentType != null && activeTab.Content == null)
            {
                try
                {
                    FrameworkElement content = (FrameworkElement)Activator.CreateInstance(activeTab.DynamicContentType);
                    content.Margin = activeTab.DynamicContentMargin;
                    activeTab.Content = content;
                }
                catch (Exception ex)
                {
                    if ((ex is TargetInvocationException) && ex.InnerException != null)
                    {
                        MessageHelper.ShowErrorOnControlInit(ex.InnerException, this);
                    }
                    else
                    {
                        MessageHelper.ShowErrorOnControlInit(ex, this);
                    }
                    activeTab.Content = "Error loading the content";
                }
            }
        }
    }
}
