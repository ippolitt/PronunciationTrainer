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
using Pronunciation.Trainer.Views;

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for TrainingSelectionDialog.xaml
    /// </summary>
    public partial class TrainingSelectionDialog : Window
    {
        public TrainingListItem SelectedTraining { get; private set; }

        public TrainingSelectionDialog()
        {
            InitializeComponent();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            DataContext = PronunciationDbContext.Instance;

            var sort = trainingsDataGrid.Items.SortDescriptions;
            sort.Add(new SortDescription("Created", ListSortDirection.Descending));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (trainingsDataGrid.SelectedItems.Count != 1)
            {
                MessageBox.Show("You must select exactly one training!", "Select training");
                return;
            }

            SelectedTraining = (TrainingListItem)trainingsDataGrid.SelectedItems[0];
            this.DialogResult = true;
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            SelectedTraining = null;
            this.DialogResult = false;
            this.Close();
        }

        private void trainingsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TrainingListItem activeRecord = trainingsDataGrid.SelectedItem as TrainingListItem;
            if (activeRecord == null)
                return;

            SelectedTraining = activeRecord;
            this.DialogResult = true;
            this.Close();
        }
    }
}
