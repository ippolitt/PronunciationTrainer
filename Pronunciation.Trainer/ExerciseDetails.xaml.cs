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
using Pronunciation.Core.Database;
using System.Data.Entity;
using System.Data.Linq;
using System.ComponentModel;
using Pronunciation.Core.Providers;
using Pronunciation.Trainer.AudioContexts;
using Pronunciation.Core;

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for ExerciseDetails.xaml
    /// </summary>
    public partial class ExerciseDetails : Window
    {
        public bool CreateNew { get; set; }
        public int? ExerciseId { get; set; }

        private Entities _dbRecordContext;
        private TrainingAudioContext _audioContext;
        private TrainingProvider _provider;
        private Exercise _activeRecord;

        public ExerciseDetails()
        {
            InitializeComponent();
        }

        public Exercise ActiveRecord
        {
            get
            {
                if (_activeRecord == null)
                {
                    if (CreateNew)
                    {
                        _activeRecord = _dbRecordContext.Exercises.Create();
                        _dbRecordContext.Exercises.Add(_activeRecord);
                    }
                    else
                    {
                        _activeRecord = _dbRecordContext.Exercises.Single(x => x.ExerciseId == ExerciseId.Value);
                    }
                }
                return _activeRecord;
            }
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            _dbRecordContext = new Entities();
            _provider = new TrainingProvider(AppSettings.Instance.BaseFolder);

            exerciseTypeIdComboBox.Items.SortDescriptions.Add(new SortDescription("Ordinal", ListSortDirection.Ascending));
            topicIdComboBox.Items.SortDescriptions.Add(new SortDescription("Ordinal", ListSortDirection.Ascending));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ExerciseId exerciseId = BuildExerciseId(ActiveRecord);

            _audioContext = new TrainingAudioContext(_provider, exerciseId);
            audioPanel.AttachContext(_audioContext);
            lstRecords.AttachPanel(audioPanel);

            LoadExercise(exerciseId);
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            // In case user pressed "Enter" and current focus is in a textbox
            btnOK.Focus();
            SaveChanges();
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            // In case user pressed "Esc" and current focus is in a textbox
            btnCancel.Focus();
            if (_dbRecordContext.HasChanges())
            {
                var result = MessageBox.Show(
                    "You have some pending changes. Are you sure you want to discard them?",
                    "Confirm discarding changes",
                    MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                    return;
            }

            this.Close();
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            btnApply.Focus();
            SaveChanges();

            ExerciseId exerciseId = BuildExerciseId(ActiveRecord);
            _audioContext.ResetExerciseContect(exerciseId);
            LoadExercise(exerciseId);
        }

        private void SaveChanges()
        {
            if (_dbRecordContext.HasChanges())
            {
                _dbRecordContext.SaveChanges();
                ((PronunciationDbContext)DataContext).NotifyExerciseChanged(ActiveRecord.ExerciseId, CreateNew);
            }
        }

        private void LoadExercise(ExerciseId exerciseId)
        {
            if (exerciseId == null)
            {
                imgContent.Source = null;
                lstRecords.ItemsSource = null;
                return;
            }

            var imageUrl = _provider.GetExerciseImagePath(exerciseId);
            if (imageUrl != null)
            {
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                image.UriSource = imageUrl;
                image.EndInit();

                imgContent.Source = image;
            }
            else
            {
                imgContent.Source = null;
            }

            lstRecords.ItemsSource = _provider.GetReferenceAudioList(exerciseId);
        }

        private void lstRecords_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = lstRecords.SelectedItem as KeyTextPair<string>;
            if (selectedItem == null)
                return;

            _audioContext.RefreshContext(selectedItem.Key, true);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            audioPanel.StopAction(false);
        }

        private ExerciseId BuildExerciseId(Exercise record)
        {
            if (!record.BookId.HasValue || !record.SourceCD.HasValue || !record.SourceTrack.HasValue)
                return null;

            return new ExerciseId
            {
                BookId = record.BookId.Value,
                CDNumber = record.SourceCD.Value,
                TrackNumber = record.SourceTrack.Value
            };
        }
    }
}
