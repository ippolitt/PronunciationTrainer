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
    }
}
