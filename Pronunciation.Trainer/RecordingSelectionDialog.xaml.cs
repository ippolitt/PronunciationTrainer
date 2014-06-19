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
using System.Windows.Shapes;
using System.ComponentModel;
using Pronunciation.Core.Database;

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for RecordingSelectionDialog.xaml
    /// </summary>
    public partial class RecordingSelectionDialog : Window
    {
        public Recording SelectedRecording { get; private set; }

        public RecordingSelectionDialog()
        {
            InitializeComponent();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            DataContext = PronunciationDbContext.Instance;

            var sort = recordingsDataGrid.Items.SortDescriptions;
            sort.Add(new SortDescription("Created", ListSortDirection.Descending));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (recordingsDataGrid.SelectedItems.Count != 1)
            {
                MessageBox.Show("You must select exactly one recording!", "Select recording");
                return;
            }

            SelectedRecording = (Recording)recordingsDataGrid.SelectedItems[0];
            this.DialogResult = true;
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            SelectedRecording = null;
            this.DialogResult = false;
            this.Close();
        }

        private void recordingsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Recording activeRecord = recordingsDataGrid.SelectedItem as Recording;
            if (activeRecord == null)
                return;

            SelectedRecording = activeRecord;
            this.DialogResult = true;
            this.Close();
        }
    }
}
