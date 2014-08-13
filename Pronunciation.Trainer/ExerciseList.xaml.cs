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
using Pronunciation.Core.Database;
using System.Data.Linq;
using System.ComponentModel;
using Pronunciation.Core.Providers;
using Pronunciation.Trainer.Views;
using Pronunciation.Core.Providers.Exercise;

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for ExerciseList.xaml
    /// </summary>
    public partial class ExerciseList : UserControl
    {
        public ExerciseList()
        {
            InitializeComponent();
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            DataContext = PronunciationDbContext.Instance;

            var sort = exerciseDataGrid.Items.SortDescriptions;
            sort.Add(new SortDescription("BookId", ListSortDirection.Ascending));
            sort.Add(new SortDescription("TrackDisplayName", ListSortDirection.Ascending));
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void exerciseDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ExerciseListItem activeRecord = exerciseDataGrid.SelectedItem as ExerciseListItem;
            if (activeRecord == null)
                return;

            ExerciseDetails dialog = new ExerciseDetails();
            dialog.ExerciseId = activeRecord.ExerciseId;
            dialog.Show();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            ExerciseDetails dialog = new ExerciseDetails();
            dialog.CreateNew = true;
            dialog.Show();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (exerciseDataGrid.SelectedItems.Count <= 0)
                return;

            var result = MessageBox.Show(
                "Are you sure that you want to delete the selected records? All the assosiated audios will be deleted as well.",
                "Confirm deletion", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                var exercisesToDelete = exerciseDataGrid.SelectedItems.Cast<ExerciseListItem>().ToArray();
                var exerciseAudios = PronunciationDbContext.Instance.GetExerciseAudios(exercisesToDelete.Select(x => x.ExerciseId).ToArray());
                
                PronunciationDbContext.Instance.RemoveExercises(exercisesToDelete);
                AppSettings.Instance.Recorders.Exercise.DeleteTargetAudios(
                    exerciseAudios.Select(x => new ExerciseTargetKey(x.ExerciseId, x.AudioName)));
            }
        }
    }
}
