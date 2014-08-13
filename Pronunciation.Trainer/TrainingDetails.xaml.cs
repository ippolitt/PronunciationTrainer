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
using Pronunciation.Trainer.AudioContexts;
using Pronunciation.Core.Providers.Recording;
using Pronunciation.Core.Providers.Training;
using System.ComponentModel;
using Pronunciation.Core;
using System.Diagnostics;
using System.IO;
using Pronunciation.Core.Providers.Recording.HistoryPolicies;
using Microsoft.Win32;

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for TrainingDetails.xaml
    /// </summary>
    public partial class TrainingDetails : Window
    {
        public bool CreateNew { get; set; }
        public bool NeedsDialogResult { get; set; }
        public Guid? TrainingId { get; set; }

        private Entities _dbRecordContext;
        private TrainingAudioContext _audioContext;
        private RecordingProviderWithTargetKey<TrainingTargetKey> _recordingProvider;
        private Training _activeRecord;
        private readonly CollectionChangeTracker<string> _audioKeysTracker;

        private static readonly string ContentFormat = DataFormats.Rtf;

        public TrainingDetails()
        {
            _audioKeysTracker = new CollectionChangeTracker<string>(StringComparer.OrdinalIgnoreCase);
            InitializeComponent();
        }

        public Training ActiveRecord
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

            audioPanel.RecordingCompleted += AudioPanel_RecordingCompleted;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_activeRecord == null)
            {
                _activeRecord = InitActiveRecord();   
            }

            _recordingProvider = new RecordingProviderWithTargetKey<TrainingTargetKey>(
                AppSettings.Instance.Recorders.Training,
                new TrainingTargetKey(_activeRecord.TrainingId),
                new AlwaysAddRecordingPolicy());
            _audioContext = new TrainingAudioContext(_recordingProvider);
            audioPanel.AttachContext(_audioContext);

            LoadContent(_activeRecord);

            lstRecordings.AttachPanel(audioPanel);
            lstRecordings.AttachItemsSource(_recordingProvider.GetAudioList());
            if (lstRecordings.Items.Count > 0)
            {
                lstRecordings.SelectedIndex = 0;
                lstRecordings.Focus();
            }
            else
            {
                _audioContext.RefreshContext(_activeRecord.ReferenceAudioData, null, false);
                SetListButtonsState(false);
                txtTitle.Focus();
            }

            btnDeleteReference.IsEnabled = _activeRecord.ReferenceAudioData != null;
            btnApply.IsEnabled = !NeedsDialogResult;
        }

        private Training InitActiveRecord()
        {
            Training activeRecord;
            if (CreateNew)
            {
                activeRecord = _dbRecordContext.Trainings.Create();
                activeRecord.TrainingId = Guid.NewGuid();
                TrainingId = activeRecord.TrainingId;
                _dbRecordContext.Trainings.Add(activeRecord);
            }
            else
            {
                activeRecord = _dbRecordContext.Trainings.Single(x => x.TrainingId == TrainingId.Value);
            }

            return activeRecord;
        }

        private void lstRecordings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshAudioContext(false);
            SetListButtonsState(true);
        }

        private void lstRecordings_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            RefreshAudioContext(true);
        }

        private void RefreshAudioContext(bool playAudio)
        {
            _audioContext.RefreshContext(_activeRecord.ReferenceAudioData,
                lstRecordings.SelectedRecording == null ? null : lstRecordings.SelectedRecording.AudioKey, 
                playAudio);
        }

        private void SetListButtonsState(bool isEnabled)
        {
            btnDeleteRecorded.IsEnabled = isEnabled;
        }

        private void AudioPanel_RecordingCompleted(string recordedFilePath, bool isTemporaryFile)
        {
            string audioKey = _recordingProvider.RegisterNewAudio(DateTime.Now, recordedFilePath);
            _audioKeysTracker.RegisterAddedItem(audioKey);
            // In case if audio with the same key has been previously deleted we unregister it
            _audioKeysTracker.UnregisterDeletedItem(audioKey);

            lstRecordings.AttachItemsSource(GetRecordedAudiosExceptDeleted());
            lstRecordings.SelectedRecording = lstRecordings.Recordings.Single(x => x.AudioKey == audioKey);
            lstRecordings.Focus();
        }

        private RecordedAudioListItem[] GetRecordedAudiosExceptDeleted()
        {
            RecordedAudioListItem[] allAudios = _recordingProvider.GetAudioList();
            if (_audioKeysTracker.HasDeletedItems)
            {
                string[] deletedAudios = _audioKeysTracker.GetDeletedItems();
                return allAudios.Where(x => !deletedAudios.Contains(x.AudioKey, StringComparer.OrdinalIgnoreCase)).ToArray();
            }
            else
            {
                return allAudios;
            }
        }

        private void btnDeleteRecorded_Click(object sender, RoutedEventArgs e)
        {
            if (lstRecordings.SelectedRecordingsCount <= 0)
                return;

            _audioKeysTracker.RegisterDeletedItems(lstRecordings.SelectedRecordings.Select(x => x.AudioKey));
            lstRecordings.RemoveSelected();
            if (lstRecordings.Items.Count > 0)
            {
                lstRecordings.SelectedIndex = 0;
                lstRecordings.Focus();
            }
            else
            {
                _audioContext.RefreshContext(_activeRecord.ReferenceAudioData, null, false);
                SetListButtonsState(false);
            }
        }

        private void btnImportReference_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Audio files (*.mp3)|*.mp3|All files (*.*)|*.*";

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                _activeRecord.ReferenceAudioData = File.ReadAllBytes(dlg.FileName);
                _activeRecord.ReferenceAudioName = Path.GetFileNameWithoutExtension(dlg.FileName);
                txtReferenceAudio.Text = _activeRecord.ReferenceAudioName;

                RefreshAudioContext(false);
                btnDeleteReference.IsEnabled = true;
            }
        }

        private void btnDeleteReference_Click(object sender, RoutedEventArgs e)
        {
            _activeRecord.ReferenceAudioData = null;
            _activeRecord.ReferenceAudioName = null;
            txtReferenceAudio.Text = null;

            RefreshAudioContext(false);
            btnDeleteReference.IsEnabled = false;
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

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            btnApply.Focus();
            SaveChanges();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SaveChanges()
        {
            SetContent(_activeRecord);
            if (_dbRecordContext.HasChanges())
            {
                if (CreateNew)
                {
                    _activeRecord.Created = DateTime.Now;
                }
                _dbRecordContext.SaveChanges();
                PronunciationDbContext.Instance.NotifyTrainingChanged(_activeRecord.TrainingId, CreateNew);
                CreateNew = false;
            }

            // Commit deleted audios
            // (added audios are already in the database so just remove them from the tracker with 'Reset' method)
            if (_audioKeysTracker.HasDeletedItems)
            {
                _recordingProvider.DeleteAudios(_audioKeysTracker.GetDeletedItems());
            }
            _audioKeysTracker.Reset();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            audioPanel.StopAction(false);

            // This will commit latest changes in case if focus is in a textbox
            btnCancel.Focus();

            if (_audioKeysTracker.HasDeletedItems || _audioKeysTracker.HasAddedItems || _dbRecordContext.HasChanges())
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

                // Uncommit added audios
                // (deleted audios are not yet deleted in the database so just remove them from the tracker with 'Reset' method)
                if (_audioKeysTracker.HasAddedItems)
                {
                    _recordingProvider.DeleteAudios(_audioKeysTracker.GetAddedItems());
                }
                _audioKeysTracker.Reset();
            }

            if (NeedsDialogResult)
            {
                DialogResult = !_dbRecordContext.HasChanges();
            }
        }

        private void LoadContent(Training record)
        {
            if (record.TrainingData == null)
                return;

            FlowDocument doc = rtxtContent.Document;
            var range = new TextRange(doc.ContentStart, doc.ContentEnd);
            range.Load(new MemoryStream(record.TrainingData), ContentFormat);
        }

        private void SetContent(Training record)
        {
            if (IsRichTextboxEmpty(rtxtContent))
            {
                if (record.TrainingData != null && record.TrainingData.Length > 0)
                {
                    record.TrainingData = null;
                    record.TrainingText = null;
                }
                return;
            }

            FlowDocument doc = rtxtContent.Document;
            var range = new TextRange(doc.ContentStart, doc.ContentEnd);
            using (MemoryStream buffer = new MemoryStream())
            {
                range.Save(buffer, ContentFormat);
                var content = buffer.ToArray();
                if (content.SequenceEqual(record.TrainingData ?? new byte[0]))
                {
                    return;
                }

                record.TrainingData = content;
            }

            using (MemoryStream buffer = new MemoryStream())
            {
                range.Save(buffer, DataFormats.Text);
                record.TrainingText = Encoding.UTF8.GetString(buffer.ToArray());
            }
        }

        private bool IsRichTextboxEmpty(RichTextBox rtb)
        {
            var start = rtb.Document.ContentStart;
            var end = rtb.Document.ContentEnd;
            int difference = start.GetOffsetToPosition(end);

            // When we insert a single image from the Clipboard the difference=3
            return difference == 0 || difference == 2 || difference == 4;
        }
    }
}
