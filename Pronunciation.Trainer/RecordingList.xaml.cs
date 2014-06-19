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
using System.Windows.Shapes;
using System.ComponentModel;
using Pronunciation.Core.Database;

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for RecordingList.xaml
    /// </summary>
    public partial class RecordingList : UserControl
    {
        public RecordingList()
        {
            InitializeComponent();
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            DataContext = PronunciationDbContext.Instance;
            PronunciationDbContext.Instance.RecordingChanged += _dbContext_RecordingChanged;

            var sort = recordingsDataGrid.Items.SortDescriptions;
            sort.Add(new SortDescription("Created", ListSortDirection.Descending));
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void _dbContext_RecordingChanged(Guid recordingId, bool isAdded)
        {
            recordingsDataGrid.Items.Refresh();
        }

        private void recordingsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Recording activeRecord = recordingsDataGrid.SelectedItem as Recording;
            if (activeRecord == null)
                return;

            RecordingDetails dialog = new RecordingDetails();
            dialog.RecordingId = activeRecord.RecordingId;
            dialog.Show();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            RecordingDetails dialog = new RecordingDetails();
            dialog.CreateNew = true;
            dialog.Show();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (recordingsDataGrid.SelectedItems.Count <= 0)
                return;

            var result = MessageBox.Show(
                "Are you sure that you want to delete the selected records?",
                "Confirm deletion", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                var selectedRecords = recordingsDataGrid.SelectedItems.Cast<Recording>().ToArray();
                foreach (var record in selectedRecords)
                {
                    PronunciationDbContext.Instance.Recordings.Remove(record);
                }
                PronunciationDbContext.Instance.Target.SaveChanges();
            }
        }
    }
}
