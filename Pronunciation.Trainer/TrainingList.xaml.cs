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
using Pronunciation.Trainer.Views;
using Pronunciation.Core.Providers.Training;
using Pronunciation.Trainer.Database;

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for TrainingList.xaml
    /// </summary>
    public partial class TrainingList : UserControl
    {
        public TrainingList()
        {
            InitializeComponent();
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            DataContext = PronunciationDbContext.Instance;

            var sort = trainingsDataGrid.Items.SortDescriptions;
            sort.Add(new SortDescription("Created", ListSortDirection.Descending));
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        private void trainingsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TrainingListItem activeRecord = trainingsDataGrid.SelectedItem as TrainingListItem;
            if (activeRecord == null)
                return;

            TrainingDetails dialog = new TrainingDetails();
            dialog.TrainingId = activeRecord.TrainingId;
            dialog.Show();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            TrainingDetails dialog = new TrainingDetails();
            dialog.CreateNew = true;
            dialog.Show();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (trainingsDataGrid.SelectedItems.Count <= 0)
                return;

            var result = MessageBox.Show(
                "Are you sure that you want to delete the selected records? All the assosiated audios will be deleted as well.",
                "Confirm deletion", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                var trainingsToDelete = trainingsDataGrid.SelectedItems.Cast<TrainingListItem>().ToArray();
                PronunciationDbContext.Instance.RemoveTrainings(trainingsToDelete);
                AppSettings.Instance.Recorders.Training.DeleteTargetAudios(
                    trainingsToDelete.Select(x => new TrainingTargetKey(x.TrainingId)));
            }
        }
    }
}
