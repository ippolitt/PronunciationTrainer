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
        private PronunciationDbContext _dbContext;

        public ExerciseList()
        {
            InitializeComponent();
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            _dbContext = new PronunciationDbContext();
            _dbContext.ExerciseChanged += _dbContext_ExerciseChanged;
            DataContext = _dbContext;

            var sort = exerciseDataGrid.Items.SortDescriptions;
            sort.Add(new SortDescription("BookId", ListSortDirection.Ascending));
            sort.Add(new SortDescription("SourceCD", ListSortDirection.Ascending));
            sort.Add(new SortDescription("SourceTrack", ListSortDirection.Ascending));
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            btnSave.IsEnabled = false;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_dbContext.Target.HasChanges())
            {
                _dbContext.Target.SaveChanges();
            }
            btnSave.IsEnabled = false;
        }

        public bool SaveChanges()
        {
            if (_dbContext.Target.HasChanges())
            {
                var result = MessageBox.Show(
                    "You have some pending changes in the exercise list. Do you want to save them?",
                    "Pending changes", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Cancel)
                    return false;

                if (result == MessageBoxResult.Yes)
                {
                    _dbContext.Target.SaveChanges();
                    btnSave.IsEnabled = false;
                }
            }

            return true;
        }

        private void _dbContext_ExerciseChanged(int entryId, bool isAdded)
        {
            exerciseDataGrid.Items.Refresh();
        }

        private void exerciseDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Exercise activeRecord = exerciseDataGrid.SelectedItem as Exercise;
            if (activeRecord == null)
                return;

            ExerciseDetails dialog = new ExerciseDetails();
            dialog.DataContext = _dbContext;
            dialog.ExerciseId = activeRecord.ExerciseId;
            dialog.Show();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            ExerciseDetails dialog = new ExerciseDetails();
            dialog.DataContext = _dbContext;
            dialog.CreateNew = true;
            dialog.Show();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var activeRecord = exerciseDataGrid.SelectedItem as Exercise;
            if (activeRecord == null)
                return;

            _dbContext.Exercises.Remove(activeRecord);
            btnSave.IsEnabled = true;
        }
    }
}
