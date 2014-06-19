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
using Pronunciation.Trainer.AudioContexts;
using Pronunciation.Core.Providers;
using System.ComponentModel;
using Pronunciation.Core;
using System.Diagnostics;
using System.IO;

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for RecordingDetails.xaml
    /// </summary>
    public partial class RecordingDetails : Window
    {
        public bool CreateNew { get; set; }
        public bool NeedsDialogResult { get; set; }
        public Guid? RecordingId { get; set; }

        private Entities _dbRecordContext;
        private RecordingAudioContext _audioContext;
        private RecordingProvider _provider;
        private Recording _activeRecord;

        public RecordingDetails()
        {
            InitializeComponent();
        }

        public Recording ActiveRecord
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
            _provider = new RecordingProvider(AppSettings.Instance.Folders.Recordings);

            audioPanel.RecordingCompleted += AudioPanel_RecordingCompleted;
            lstRecords.Items.SortDescriptions.Add(new SortDescription("Text", ListSortDirection.Descending));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_activeRecord == null)
            {
                InitActiveRecord();
            }

            _audioContext = new RecordingAudioContext(_provider, _activeRecord.RecordingId);
            audioPanel.AttachContext(_audioContext);
            lstRecords.AttachPanel(audioPanel);

            lstRecords.ItemsSource = _provider.GetAudioList(_activeRecord.RecordingId);
            if (lstRecords.Items.Count > 0)
            {
                lstRecords.SelectedIndex = 0;
                lstRecords.Focus();
            }
            else
            {
                txtTitle.Focus();
            }

            btnExploreFolder.IsEnabled = AudioFolderExists(_activeRecord.RecordingId);
            btnDeleteSelected.IsEnabled = lstRecords.Items.Count > 0;
            btnApply.IsEnabled = !NeedsDialogResult;
        }

        private void InitActiveRecord()
        {
            if (CreateNew)
            {
                _activeRecord = _dbRecordContext.Recordings.Create();
                _activeRecord.RecordingId = Guid.NewGuid();
                RecordingId = _activeRecord.RecordingId;
                _dbRecordContext.Recordings.Add(_activeRecord);
            }
            else
            {
                _activeRecord = _dbRecordContext.Recordings.Single(x => x.RecordingId == RecordingId.Value);
            }
        }

        private bool AudioFolderExists(Guid recordingId)
        {
            return Directory.Exists(_provider.BuildAudioFolderPath(recordingId));
        }

        private void lstRecords_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = lstRecords.SelectedItem as KeyTextPair<string>;
            if (selectedItem == null)
                return;

            _audioContext.RefreshContext(selectedItem.Key, false);
        }

        private void lstRecords_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedItem = lstRecords.SelectedItem as KeyTextPair<string>;
            if (selectedItem == null)
                return;

            _audioContext.RefreshContext(selectedItem.Key, true);
        }

        private void AudioPanel_RecordingCompleted(string recordedFilePath)
        {
            var recordingName = _provider.GetAudioName(recordedFilePath);
            lstRecords.ItemsSource = _provider.GetAudioList(_activeRecord.RecordingId);
            lstRecords.SelectedItem = lstRecords.Items.Cast<KeyTextPair<string>>().Single(x => x.Key == recordingName);
            lstRecords.Focus();

            btnExploreFolder.IsEnabled = true;
            btnDeleteSelected.IsEnabled = true;
        }

        private void btnDeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (lstRecords.SelectedItems.Count <= 0)
                return;

            var result = MessageBox.Show(
                "Are you sure that you want to delete the selected records?",
                "Confirm deletion", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                bool isSuccess = _provider.DeleteAudio(
                    _activeRecord.RecordingId,
                    lstRecords.SelectedItems.Cast<KeyTextPair<string>>().Select(x => x.Key));
                if (isSuccess)
                {
                    lstRecords.ItemsSource = _provider.GetAudioList(_activeRecord.RecordingId);
                    if (lstRecords.Items.Count > 0)
                    {
                        lstRecords.SelectedIndex = 0;
                        lstRecords.Focus();
                    }
                    else
                    {
                        _audioContext.RefreshContext(null, false);
                        btnDeleteSelected.IsEnabled = false;
                    }
                }
                else
                {
                    MessageBox.Show("Some of the files are currently in use and can't be deleted!", "Error");
                }
            }
        }

        private void btnExploreFolder_Click(object sender, RoutedEventArgs e)
        {
            string audioFolderPath = _provider.BuildAudioFolderPath(_activeRecord.RecordingId);
            if (!Directory.Exists(audioFolderPath))
                return;

            Process.Start(audioFolderPath);
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
            if (_dbRecordContext.HasChanges())
            {
                if (CreateNew)
                {
                    _activeRecord.Created = DateTime.Now;
                }
                _dbRecordContext.SaveChanges();
                CreateNew = false;

                PronunciationDbContext.Instance.NotifyRecordingChanged(_activeRecord.RecordingId, CreateNew);
            }
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
    }
}
