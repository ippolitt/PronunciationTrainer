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
using Pronunciation.Trainer.AudioContexts;
using Pronunciation.Core.Providers.Recording;
using Pronunciation.Core.Contexts;
using System.ComponentModel;
using Pronunciation.Trainer.Export;
using Pronunciation.Trainer.Utility;

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for RecordingHistory.xaml
    /// </summary>
    public partial class RecordingHistory : Window
    {
        private RecordingProviderWithTargetKey _recordingProvider;
        private RecordingHistoryAudioContext _audioContext;

        public RecordingHistory()
        {
            InitializeComponent();
        }

        public void InitContext(RecordingProviderWithTargetKey recordingProvider, PlaybackData referenceAudio)
        {
            _recordingProvider = recordingProvider;
            _audioContext = new RecordingHistoryAudioContext(recordingProvider, referenceAudio);
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            audioPanel.RecordingCompleted += AudioPanel_RecordingCompleted;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_audioContext == null)
                throw new ArgumentException("Audio context is not initialized!");

            audioPanel.AttachContext(_audioContext);

            lstRecordings.RecordingsDeleteRequested += lstRecordings_RecordingsDeleteRequested;
            lstRecordings.AttachPanel(audioPanel);
            lstRecordings.AttachItemsSource(_recordingProvider.GetAudioList());
            if (lstRecordings.Items.Count > 0)
            {
                lstRecordings.SelectedIndex = 0;
            }
            else
            {
                SetListButtonsState(false);
            }
            lstRecordings.CaptureKeyboardFocus();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            audioPanel.StopAction(false);
        }

        private void OnCloseCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        private void AudioPanel_RecordingCompleted(string recordedFilePath, bool isTemporaryFile)
        {
            string audioKey = _recordingProvider.RegisterNewAudio(DateTime.Now, recordedFilePath);

            lstRecordings.AttachItemsSource(_recordingProvider.GetAudioList());
            lstRecordings.SelectedRecording = lstRecordings.Recordings.Single(x => x.AudioKey == audioKey);
            lstRecordings.Focus();
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

        private void lstRecordings_RecordingsDeleteRequested(object sender, EventArgs e)
        {
            DeleteRecordings();
        }

        private void RefreshAudioContext(bool playAudio)
        {
            var selectedRecording = lstRecordings.SelectedRecordingsCount > 1 ? null : lstRecordings.SelectedRecording;
            if (selectedRecording == null)
            {
                _audioContext.ResetContext();
            }
            else
            {
                _audioContext.RefreshContext(selectedRecording, playAudio);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            DeleteRecordings();
        }

        private void DeleteRecordings()
        {
            if (lstRecordings.SelectedRecordingsCount <= 0)
                return;

            var result = MessageBox.Show(
                "Are you sure that you want to delete the selected recordings? This action cannot be undone.",
                "Confirm deletion", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                bool isSuccess = _recordingProvider.DeleteAudios(lstRecordings.SelectedRecordings.Select(x => x.AudioKey));
                if (isSuccess)
                {
                    lstRecordings.RemoveSelected();
                }
                else
                {
                    lstRecordings.AttachItemsSource(_recordingProvider.GetAudioList());
                    MessageBox.Show("Some of the selected recordings haven't been deleted!", "Warning");
                }

                if (lstRecordings.Items.Count > 0)
                {
                    lstRecordings.SelectedIndex = 0;
                    lstRecordings.Focus();
                }
                else
                {
                    _audioContext.ResetContext();
                    SetListButtonsState(false);
                }
            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            if (lstRecordings.SelectedRecordingsCount <= 0)
                return;

            var exporter = new AudioExporter(ControlsHelper.GetWindow(this));
            exporter.ExportRecordings(_recordingProvider, lstRecordings.SelectedRecordings.OrderByDescending(x => x.RecordingDate));
        }

        private void SetListButtonsState(bool isEnabled)
        {
            btnDelete.IsEnabled = isEnabled;
            btnExport.IsEnabled = isEnabled;
        }
    }
}
