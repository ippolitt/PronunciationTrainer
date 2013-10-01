﻿using System;
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
            AppSettings.Instance.Save();
            if (!exersises.SaveChanges())
            {
                e.Cancel = true;
            }
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            if (dictionary.IsVisible)
            {
                dictionary.Focus();
            }
        }
    }
}
