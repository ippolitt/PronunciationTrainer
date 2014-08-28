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
using Pronunciation.Core.Database;
using System.Data.Entity;
using System.Data.Linq;
using System.ComponentModel;
using Pronunciation.Core.Providers.Exercise;
using Pronunciation.Trainer.AudioContexts;
using Pronunciation.Core;
using System.IO;
using Pronunciation.Trainer.Views;
using Pronunciation.Core.Contexts;
using Pronunciation.Core.Providers.Recording.HistoryPolicies;
using Microsoft.Win32;
using Pronunciation.Trainer.Export;
using Pronunciation.Trainer.Utility;
using Pronunciation.Trainer.Database;
using Pronunciation.Core.Audio;
using Pronunciation.Trainer.Recording;

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for ExerciseDetails.xaml
    /// </summary>
    public partial class ExerciseDetails : Window
    {
        public bool CreateNew { get; set; }
        public Guid? ExerciseId { get; set; }

        private Entities _dbRecordContext;
        private ExerciseAudioContext _audioContext;
        private Exercise _activeRecord;
        private readonly CollectionChangeTracker<string> _audioNamesTracker;
        private bool _suppressPlay = false;

        public ExerciseDetails()
        {
            _audioNamesTracker = new CollectionChangeTracker<string>(StringComparer.OrdinalIgnoreCase);
            InitializeComponent();
        }

        // For use in XAML
        public Exercise ActiveRecord
        {
            get
            {
                if (_activeRecord == null)
                {
                    _activeRecord = InitActiveRecord();
                }
                return _activeRecord;
            }
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            _dbRecordContext = new Entities();

            exerciseTypeIdComboBox.Items.SortDescriptions.Add(new SortDescription("Ordinal", ListSortDirection.Ascending));
            topicIdComboBox.Items.SortDescriptions.Add(new SortDescription("Ordinal", ListSortDirection.Ascending));

            borderImage.BorderThickness = lstAudios.BorderThickness;
            borderImage.BorderBrush = lstAudios.BorderBrush;

            lstAudios.PreviewKeyDown += AudiosList_PreviewKeyDown;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_activeRecord == null)
            {
                _activeRecord = InitActiveRecord();
            }

            _audioContext = new ExerciseAudioContext(AppSettings.Instance.Recorders.Exercise,
                _activeRecord.ExerciseId, new AppSettingsBasedRecordingPolicy());
            audioPanel.AttachContext(_audioContext);
            audioPanel.RecordingCompleted += AudioPanel_RecordingCompleted;

            LoadContent();

            lstAudios.ItemsSource = LoadReferenceAudios();
            if (lstAudios.Items.Count > 0)
            {
                _suppressPlay = true;
                lstAudios.SelectedIndex = 0;
                _suppressPlay = false;
            }
            else
            {
                SetListButtonsState(false);
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            if (lstAudios.Items.Count > 0)
            {
                // It works only after window content has been rendered
                lstAudios.FocusSelectedItem();
            }
            else
            {
                bookIdComboBox.Focus();
            }
        }

        private void AudioPanel_RecordingCompleted(string recordedFilePath, bool isTemporaryFile)
        {
            _audioContext.RegisterRecordedAudio(recordedFilePath, DateTime.Now);
        }

        private void AudiosList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                    audioPanel.PlayReferenceAudio();
                    break;
                case Key.Right:
                    audioPanel.PlayRecordedAudio();
                    break;
            }
        }

        private Exercise InitActiveRecord()
        {
            Exercise activeRecord;
            if (CreateNew)
            {
                activeRecord = _dbRecordContext.Exercises.Create();
                activeRecord.ExerciseId = Guid.NewGuid();
                activeRecord.ExerciseAudios = new List<ExerciseAudio>();
                ExerciseId = activeRecord.ExerciseId;
                _dbRecordContext.Exercises.Add(activeRecord);
            }
            else
            {
                activeRecord = _dbRecordContext.Exercises
                    .Where(x => x.ExerciseId == ExerciseId.Value)
                    .Include(x => x.ExerciseAudios)
                    .Single();
            }

            return activeRecord;
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
            if (ControlsHelper.IsExplicitCloseRequired(btnCancel))
            {
                this.Close();
            }
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            btnApply.Focus();
            SaveChanges();
        }

        private void SaveChanges()
        {
            if (_dbRecordContext.HasChanges())
            {
                _dbRecordContext.SaveChanges();
                PronunciationDbContext.Instance.NotifyExerciseChanged(_activeRecord.ExerciseId, CreateNew);
                CreateNew = false;

                // Remove all assosiated recordings of the deleted audios from the database
                // (recordings for added audios are already in the database so just remove them from the tracker with 'Reset' method)
                if (_audioNamesTracker.HasDeletedItems)
                {
                    AppSettings.Instance.Recorders.Exercise.DeleteTargetAudios(
                        _audioNamesTracker.GetDeletedItems().Select(x => new ExerciseTargetKey(_activeRecord.ExerciseId, x)));
                }
                _audioNamesTracker.Reset();
            }
        }

        private void lstAudios_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshAudioContext(!_suppressPlay);
            SetListButtonsState(true);
        }

        private void lstAudios_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            RefreshAudioContext(true);
        }

        private void RefreshAudioContext(bool playAudio)
        {
            var selectedAudio = lstAudios.SelectedItems.Count > 1 ? null : (ExerciseAudioListItemWithData)lstAudios.SelectedItem;
            if (selectedAudio == null)
            {
                _audioContext.ResetContext();
            }
            else
            {
                _audioContext.RefreshContext(selectedAudio, playAudio);
            }
        }

        private void SetListButtonsState(bool isEnabled)
        {
            btnDeleteAudio.IsEnabled = isEnabled;
            btnExportAudio.IsEnabled = isEnabled;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            audioPanel.StopAction(false);

            // This will commit latest changes in case if focus is in a textbox
            btnCancel.Focus();

            if (_dbRecordContext.HasChanges())
            {
                if(!MessageHelper.ShowConfirmation(
                    "You have some pending changes. Are you sure you want to discard them?",
                    "Confirm discarding changes"))
                {
                    e.Cancel = true;
                    return;
                }

                // Remove all assosiated recordings of the added but discarded audios from the database
                // (deleted audios are not yet deleted in the database so just remove them from the tracker with 'Reset' method)
                if (_audioNamesTracker.HasAddedItems)
                {
                    AppSettings.Instance.Recorders.Exercise.DeleteTargetAudios(
                        _audioNamesTracker.GetAddedItems().Select(x => new ExerciseTargetKey(_activeRecord.ExerciseId, x)));
                }
                _audioNamesTracker.Reset();
            }
        }

        private void btnImportContent_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Image files (*.png;*.jpg)|*.png;*.jpg;*.jpeg|All files (*.*)|*.*";
            dlg.Title = "Select image file to import";

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                _activeRecord.ExerciseData = File.ReadAllBytes(dlg.FileName);
                LoadContent();
            }
        }

        private void btnImportAudio_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Multiselect = true;
            dlg.Filter = "Audio files (*.mp3)|*.mp3|All files (*.*)|*.*";
            dlg.Title = "Select audio files to import";

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                if (ImportReferenceAudios(dlg.FileNames))
                {
                    var currentItem = lstAudios.SelectedItem as ExerciseAudioListItemWithData;
                    ExerciseAudioListItemWithData[] audios = LoadReferenceAudios();
                    lstAudios.ItemsSource = audios;
                    _suppressPlay = true;
                    if (currentItem == null)
                    {
                        lstAudios.SelectedIndex = 0;
                    }
                    else
                    {
                        lstAudios.SelectedItem = audios.FirstOrDefault(x => x.AudioId == currentItem.AudioId);
                    }
                    _suppressPlay = false;
                    lstAudios.FocusSelectedItem();
                }
            }
        }

        private void btnExportAudio_Click(object sender, RoutedEventArgs e)
        {
            if (lstAudios.SelectedItems.Count <= 0)
                return;

            var exporter = new AudioExporter(ControlsHelper.GetWindow(this));
            exporter.ExportExerciseAudios(
                AppSettings.Instance.Recorders.Exercise, 
                lstAudios.SelectedItems.Cast<ExerciseAudioListItemWithData>().OrderBy(x => new MultipartName(x.AudioName)));
        }

        private void btnDeleteAudio_Click(object sender, RoutedEventArgs e)
        {
            if (lstAudios.SelectedItems.Count <= 0)
                return;

            if(MessageHelper.ShowConfirmation(
                "Along with the selected audios all the assosiated recordings will be deleted as well. Do you want to proceed?",
                "Confirm deletion"))
            {
                ExerciseAudioListItemWithData[] audiosToDelete = lstAudios.SelectedItems.Cast<ExerciseAudioListItemWithData>().ToArray();
                DeleteReferenceAudios(audiosToDelete);
                _audioNamesTracker.RegisterDeletedItems(audiosToDelete.Select(x => x.AudioName));

                lstAudios.ItemsSource = LoadReferenceAudios();
                if (lstAudios.Items.Count > 0)
                {
                    _suppressPlay = true;
                    lstAudios.SelectedIndex = 0;
                    _suppressPlay = false;
                    lstAudios.FocusSelectedItem();
                }
                else
                {
                    _audioContext.ResetContext();
                    SetListButtonsState(false);
                }
            }
        }

        private void LoadContent()
        {
            if (_activeRecord.ExerciseData == null)
            {
                imgContent.Source = null;
                return;
            }

            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            //image.UriSource = imageUrl;
            //image.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // required if we want to reload image from URL
            using (var imageStream = new MemoryStream(_activeRecord.ExerciseData))
            {
                image.StreamSource = imageStream;
                image.EndInit();
            }

            imgContent.Source = image;
        }

        private ExerciseAudioListItemWithData[] LoadReferenceAudios()
        {
            ICollection<ExerciseAudio> audios = _activeRecord.ExerciseAudios;
            if (audios == null || audios.Count == 0)
                return null;

            return audios.Select(x => new ExerciseAudioListItemWithData
                {
                    AudioId = x.AudioId,
                    AudioName = x.AudioName,
                    ExerciseId = x.ExerciseId,
                    Duration = x.Duration,
                    RawData = x.RawData
                })
                .OrderBy(x => new MultipartName(x.AudioName)).ToArray();
        }

        private bool ImportReferenceAudios(string[] importedFiles)
        {
            if (importedFiles == null || importedFiles.Length == 0)
                return false;

            bool hasChanges = false;
            ICollection<ExerciseAudio> currentAudios = _activeRecord.ExerciseAudios;
            foreach (string importedFile in importedFiles)
            {
                string importedAudioName = Path.GetFileNameWithoutExtension(importedFile);
                ExerciseAudio audio = currentAudios.SingleOrDefault(x => 
                    string.Equals(x.AudioName, importedAudioName, StringComparison.OrdinalIgnoreCase));
                if (audio == null)
                {
                    audio = new ExerciseAudio
                    {
                        AudioId = Guid.NewGuid(),
                        ExerciseId = _activeRecord.ExerciseId
                    };
                    currentAudios.Add(audio);
                    _audioNamesTracker.RegisterAddedItem(importedAudioName);
                    // In case if audio with the same name has been previously deleted we unregister it
                    _audioNamesTracker.UnregisterDeletedItem(importedAudioName);
                }
                audio.AudioName = importedAudioName;
                audio.RawData = File.ReadAllBytes(importedFile);
                audio.Duration = AudioHelper.GetAudioLengthMs(audio.RawData); 
                hasChanges = true;
            }

            return hasChanges;
        }

        private void DeleteReferenceAudios(IEnumerable<ExerciseAudioListItemWithData> audiosToDelete)
        {
            ICollection<ExerciseAudio> currentAudios = _activeRecord.ExerciseAudios;
            foreach (var audioToDelete in audiosToDelete)
            {
                ExerciseAudio audio = currentAudios.Single(x => x.AudioId == audioToDelete.AudioId);
                _dbRecordContext.ExerciseAudios.Remove(audio);
                _activeRecord.ExerciseAudios.Remove(audio);
            }
        }
    }
}
