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
            sort.Add(new SortDescription("SourceCD", ListSortDirection.Ascending));
            sort.Add(new SortDescription("SourceTrack", ListSortDirection.Ascending));
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void exerciseDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Exercise activeRecord = exerciseDataGrid.SelectedItem as Exercise;
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
                "Are you sure that you want to delete the selected records?",
                "Confirm deletion", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                PronunciationDbContext.Instance.RemoveExercises(exerciseDataGrid.SelectedItems.Cast<Exercise>().ToArray());
            }
        }
    }
}
