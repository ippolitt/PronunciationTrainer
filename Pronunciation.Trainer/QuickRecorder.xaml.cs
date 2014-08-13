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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Pronunciation.Trainer.AudioContexts;
using Pronunciation.Core.Providers.Training;
using Pronunciation.Core.Providers.Recording;
using System.ComponentModel;
using Pronunciation.Core;
using System.IO;
using Pronunciation.Core.Providers.Recording.HistoryPolicies;

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for Recorder.xaml
    /// </summary>
    public partial class QuickRecorder : UserControlExt, ISupportsKeyboardFocus
    {
        private RecordingProviderWithTargetKey<QuickRecorderTargetKey> _recordingProvider;
        private QuickRecorderAudioContext _audioContext;

        public QuickRecorder()
        {
            InitializeComponent();
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            _recordingProvider = new RecordingProviderWithTargetKey<QuickRecorderTargetKey>(
                AppSettings.Instance.Recorders.QuickRecorder, 
                new QuickRecorderTargetKey(),
                new AlwaysAddRecordingPolicy());
            _audioContext = new QuickRecorderAudioContext(_recordingProvider);

            audioPanel.RecordingCompleted += AudioPanel_RecordingCompleted;
            audioPanel.AttachContext(_audioContext);

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
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // We must put focus somewhere inside the control otherwise hot keys don't work
            lstRecordings.Focus();
        }

        public void CaptureKeyboardFocus()
        {
            lstRecordings.Focus();
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

        private void RefreshAudioContext(bool playAudio)
        {
            if (lstRecordings.SelectedRecording != null)
            {
                _audioContext.RefreshContext(lstRecordings.SelectedRecording.AudioKey, playAudio);
            }
        }

        private void btnDeleteSelected_Click(object sender, RoutedEventArgs e)
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

                ResetSelectedRecording();
            }
        }

        private void btnCopyToNew_Click(object sender, RoutedEventArgs e)
        {
            if (lstRecordings.SelectedRecordingsCount <= 0)
                return;

            var dialog = new TrainingDetails();
            dialog.NeedsDialogResult = true;
            dialog.CreateNew = true;
            if (dialog.ShowDialog() == true && dialog.ActiveRecord != null)
            {
                MoveSelectedRecordingsToTraining(dialog.ActiveRecord.TrainingId, dialog.ActiveRecord.Title);
            }
        }

        private void btnCopyToExisting_Click(object sender, RoutedEventArgs e)
        {
            if (lstRecordings.SelectedRecordingsCount <= 0)
                return;

            var dialog = new TrainingSelectionDialog();
            if (dialog.ShowDialog() == true && dialog.SelectedTraining != null)
            {
                MoveSelectedRecordingsToTraining(dialog.SelectedTraining.TrainingId, dialog.SelectedTraining.Title);
            }
        }

        private void MoveSelectedRecordingsToTraining(Guid trainingId, string trainingTitle)
        {
            bool isSuccess = _recordingProvider.MoveAudios(new TrainingTargetKey(trainingId), 
                lstRecordings.SelectedRecordings.Select(x => x.AudioKey));
            if (isSuccess)
            {
                lstRecordings.RemoveSelected();
                MessageBox.Show(string.Format(
                    "Succesfully moved the selected recordings to the training '{0}'.", trainingTitle), "Success");
            }
            else
            {
                lstRecordings.AttachItemsSource(_recordingProvider.GetAudioList());
                MessageBox.Show(string.Format(
                    "Some of the selected recordings haven't been moved to the training '{0}'.", trainingTitle), "Warning");    
            }

            ResetSelectedRecording();
        }

        private void ResetSelectedRecording()
        {
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

        private void SetListButtonsState(bool isEnabled)
        {
            btnDeleteSelected.IsEnabled = isEnabled;
            btnCopyToExisting.IsEnabled = isEnabled;
            btnCopyToNew.IsEnabled = isEnabled;
        }
    }
}
