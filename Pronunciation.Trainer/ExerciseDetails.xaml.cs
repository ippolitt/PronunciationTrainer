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
        public bool NeedsDialogResult { get; set; }
        public Guid? ExerciseId { get; set; }

        private bool _silentSelectionChange;
        private Entities _dbRecordContext;
        private TrainingAudioContext _audioContext;
        private TrainingProvider _provider;
        private Exercise _activeRecord;

        public ExerciseDetails()
        {
            InitializeComponent();
        }

        // For use in XAML
        public Exercise ActiveRecord
        {
            get
            {
                if (_activeRecord == null)
                {
                    InitActiveRecord();
                }
                return _activeRecord;
            }
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            _dbRecordContext = new Entities();
            _provider = new TrainingProvider(AppSettings.Instance.Folders.Exercises, AppSettings.Instance.Folders.ExercisesRecordings);

            exerciseTypeIdComboBox.Items.SortDescriptions.Add(new SortDescription("Ordinal", ListSortDirection.Ascending));
            topicIdComboBox.Items.SortDescriptions.Add(new SortDescription("Ordinal", ListSortDirection.Ascending));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_activeRecord == null)
            {
                InitActiveRecord();
            }
            ExerciseKey exerciseKey = BuildExerciseKey(_activeRecord);

            _audioContext = new TrainingAudioContext(_provider, exerciseKey);
            audioPanel.AttachContext(_audioContext);
            lstRecords.AttachPanel(audioPanel);

            LoadExercise(exerciseKey);
            if (lstRecords.Items.Count > 0)
            {
                _silentSelectionChange = true;
                lstRecords.SelectedIndex = 0;
                lstRecords.Focus();
            }
        }

        private void InitActiveRecord()
        {
            if (CreateNew)
            {
                _activeRecord = _dbRecordContext.Exercises.Create();
                _activeRecord.ExerciseId = Guid.NewGuid();
                ExerciseId = _activeRecord.ExerciseId;
                _dbRecordContext.Exercises.Add(_activeRecord);
            }
            else
            {
                _activeRecord = _dbRecordContext.Exercises.Single(x => x.ExerciseId == ExerciseId.Value);
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            // In case user pressed "Enter" and current focus is in a textbox
            btnOK.Focus();

            SaveChanges();
            if (NeedsDialogResult)
            {
                DialogResult = true;
            }
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            btnApply.Focus();
            SaveChanges();

            ExerciseKey exerciseKey = BuildExerciseKey(_activeRecord);
            _audioContext.ResetExerciseContext(exerciseKey);
            LoadExercise(exerciseKey);
        }

        private void SaveChanges()
        {
            if (_dbRecordContext.HasChanges())
            {
                _dbRecordContext.SaveChanges();
                CreateNew = false;

                PronunciationDbContext.Instance.NotifyExerciseChanged(_activeRecord.ExerciseId, CreateNew);
            }
        }

        private void LoadExercise(ExerciseKey exerciseKey)
        {
            if (exerciseKey == null)
            {
                imgContent.Source = null;
                lstRecords.ItemsSource = null;
                return;
            }

            var imageUrl = _provider.GetExerciseImagePath(exerciseKey);
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

            lstRecords.ItemsSource = _provider.GetReferenceAudioList(exerciseKey);
        }

        private void lstRecords_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_silentSelectionChange)
            {
                _silentSelectionChange = false;
                RefreshAudioContext(false);
            }
            else
            {
                RefreshAudioContext(true);
            }
        }

        private void lstRecords_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            RefreshAudioContext(true);
        }

        private void RefreshAudioContext(bool playAudio)
        {
            var selectedItem = lstRecords.SelectedItem as KeyTextPair<string>;
            if (selectedItem == null)
                return;

            _audioContext.RefreshContext(selectedItem.Key, playAudio);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            audioPanel.StopAction(false);

            // This will commit latest changes in case if focus is in a textbox
            btnCancel.Focus();

            if (_dbRecordContext.HasChanges())
            {
                var result = MessageBox.Show(
                    "You have some pending changes. Are you sure you want to discard them?",
                    "Confirm discarding changes",
                    MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            if (NeedsDialogResult)
            {
                DialogResult = !_dbRecordContext.HasChanges();
            }
        }

        private ExerciseKey BuildExerciseKey(Exercise record)
        {
            if (record.Book == null || !record.SourceCD.HasValue || !record.SourceTrack.HasValue)
                return null;

            return new ExerciseKey
            {
                BookKey = record.Book.ShortName,
                CDNumber = record.SourceCD.Value,
                TrackNumber = record.SourceTrack.Value
            };
        }
    }
}
