﻿using System;
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

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for ExerciseDetails.xaml
    /// </summary>
    public partial class ExerciseDetails : Window
    {
        private class ExerciseAudioDetails
        {
            public Guid AudioId { get; set; }
            public string AudioName { get; set; }
            public byte[] RawData { get; set; }

            public string Text
            {
                get { return AudioName; }
            }

            public override string ToString()
            {
                return Text;
            }
        }

        public bool CreateNew { get; set; }
        public bool NeedsDialogResult { get; set; }
        public Guid? ExerciseId { get; set; }

        private Entities _dbRecordContext;
        private ExerciseAudioContext _audioContext;
        private Exercise _activeRecord;
        private readonly CollectionChangeTracker<string> _audioNamesTracker;

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
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_activeRecord == null)
            {
                _activeRecord = InitActiveRecord();
            }

            _audioContext = new ExerciseAudioContext(AppSettings.Instance.Recorders.Exercise, _activeRecord.ExerciseId,
                new AlwaysOverrideRecordingPolicy());
            audioPanel.AttachContext(_audioContext);
            audioPanel.RecordingCompleted += AudioPanel_RecordingCompleted;

            LoadContent();

            lstAudios.AttachPanel(audioPanel);
            lstAudios.ItemsSource = LoadReferenceAudios();
            if (lstAudios.Items.Count > 0)
            {
                lstAudios.SelectedIndex = 0;
                lstAudios.Focus();
            }
            else
            {
                SetAudioButtonsState(false);
                bookIdComboBox.Focus();
            }
        }

        private void AudioPanel_RecordingCompleted(string recordedFilePath, bool isTemporaryFile)
        {
            _audioContext.RegisterRecordedAudio(recordedFilePath, DateTime.Now);
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
            RefreshAudioContext(false);
            SetAudioButtonsState(true);
        }

        private void lstAudios_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            RefreshAudioContext(true);
        }

        private void RefreshAudioContext(bool playAudio)
        {
            var selectedItem = lstAudios.SelectedItem as ExerciseAudioDetails;
            if (selectedItem == null)
                return;

            _audioContext.RefreshContext(selectedItem.AudioName, new PlaybackData(selectedItem.RawData), playAudio);
        }

        private void SetAudioButtonsState(bool isEnabled)
        {
            btnDeleteAudio.IsEnabled = isEnabled;
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

                // Remove all assosiated recordings of the added but discarded audios from the database
                // (deleted audios are not yet deleted in the database so just remove them from the tracker with 'Reset' method)
                if (_audioNamesTracker.HasAddedItems)
                {
                    AppSettings.Instance.Recorders.Exercise.DeleteTargetAudios(
                        _audioNamesTracker.GetAddedItems().Select(x => new ExerciseTargetKey(_activeRecord.ExerciseId, x)));
                }
                _audioNamesTracker.Reset();
            }

            if (NeedsDialogResult)
            {
                DialogResult = !_dbRecordContext.HasChanges();
            }
        }

        private void btnImportContent_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Image files (*.png;*.jpg)|*.png;*.jpg;*.jpeg|All files (*.*)|*.*";

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

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                if (ImportReferenceAudios(dlg.FileNames))
                {
                    var currentItem = lstAudios.SelectedItem as ExerciseAudioDetails;
                    ExerciseAudioDetails[] audios = LoadReferenceAudios();
                    lstAudios.ItemsSource = audios;
                    if (currentItem == null)
                    {
                        lstAudios.SelectedIndex = 0;
                    }
                    else
                    {
                        lstAudios.SelectedItem = audios.FirstOrDefault(x => x.AudioId == currentItem.AudioId);
                    }
                    lstAudios.Focus();
                }
            }
        }

        private void btnDeleteAudio_Click(object sender, RoutedEventArgs e)
        {
            if (lstAudios.SelectedItems.Count <= 0)
                return;

            var result = MessageBox.Show(
                "Along with the selected audios all the assosiated recordings will be deleted as well. Do you want to proceed?",
                "Confirm deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (result == MessageBoxResult.Yes)
            {
                ExerciseAudioDetails[] audiosToDelete = lstAudios.SelectedItems.Cast<ExerciseAudioDetails>().ToArray();
                DeleteReferenceAudios(audiosToDelete);
                _audioNamesTracker.RegisterDeletedItems(audiosToDelete.Select(x => x.AudioName));

                lstAudios.ItemsSource = LoadReferenceAudios();
                if (lstAudios.Items.Count > 0)
                {
                    lstAudios.SelectedIndex = 0;
                    lstAudios.Focus();
                }
                else
                {
                    SetAudioButtonsState(false);
                    _audioContext.ResetContext();
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

        private ExerciseAudioDetails[] LoadReferenceAudios()
        {
            ICollection<ExerciseAudio> audios = _activeRecord.ExerciseAudios;
            if (audios == null || audios.Count == 0)
                return null;

            return audios.Select(x => new ExerciseAudioDetails
                {
                    AudioId = x.AudioId,
                    AudioName = x.AudioName,
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
                        AudioName = importedAudioName,
                        ExerciseId = _activeRecord.ExerciseId
                    };
                    currentAudios.Add(audio);
                    _audioNamesTracker.RegisterAddedItem(importedAudioName);
                    // In case if audio with the same name has been previously deleted we unregister it
                    _audioNamesTracker.UnregisterDeletedItem(importedAudioName);
                }
                else
                {
                    // Update audio name in case it differs only be case
                    audio.AudioName = importedAudioName;
                }
                audio.RawData = File.ReadAllBytes(importedFile);
                hasChanges = true;
            }

            return hasChanges;
        }

        private void DeleteReferenceAudios(IEnumerable<ExerciseAudioDetails> audiosToDelete)
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
