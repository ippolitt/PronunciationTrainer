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
        private IRecordingProvider<QuickRecorderTargetKey> _recordingProvider;
        private QuickRecorderTargetKey _recorderKey;
        private QuickRecorderAudioContext _audioContext;

        public QuickRecorder()
        {
            InitializeComponent();
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            _recordingProvider = AppSettings.Instance.Recorders.QuickRecorder;
            _recorderKey = new QuickRecorderTargetKey();
            _audioContext = new QuickRecorderAudioContext(_recordingProvider, _recorderKey, new AlwaysAddRecordingPolicy());

            audioPanel.RecordingCompleted += AudioPanel_RecordingCompleted;
            lstAudios.Items.SortDescriptions.Add(new SortDescription("Text", ListSortDirection.Descending));

            audioPanel.AttachContext(_audioContext);
            lstAudios.AttachPanel(audioPanel);

            lstAudios.ItemsSource = _recordingProvider.GetAudioList(_recorderKey);
            if (lstAudios.Items.Count > 0)
            {
                lstAudios.SelectedIndex = 0;
            }
            else
            {
                SetAudioButtonsState(false);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // We must put focus somewhere inside the control otherwise hot keys don't work
            lstAudios.Focus();
        }

        public void CaptureKeyboardFocus()
        {
            lstAudios.Focus();
        }

        private void AudioPanel_RecordingCompleted(string recordedFilePath, bool isTemporaryFile)
        {
            string audioKey = _audioContext.RegisterRecordedAudio(recordedFilePath, DateTime.Now);

            lstAudios.ItemsSource = _recordingProvider.GetAudioList(_recorderKey);
            lstAudios.SelectedItem = lstAudios.Items.Cast<RecordedAudioListItem>().Single(x => x.AudioKey == audioKey);
            lstAudios.Focus();
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
            var selectedItem = lstAudios.SelectedItem as RecordedAudioListItem;
            if (selectedItem == null)
                return;

            _audioContext.RefreshContext(selectedItem.AudioKey, playAudio);
        }

        private void btnDeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (lstAudios.SelectedItems.Count <= 0)
                return;

            var result = MessageBox.Show(
                "Are you sure that you want to delete the selected audios?",
                "Confirm deletion", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                bool isSuccess = _recordingProvider.DeleteAudios(_recorderKey,
                    lstAudios.SelectedItems.Cast<RecordedAudioListItem>().Select(x => x.AudioKey));
                RefreshList();
                if (!isSuccess)
                {
                    MessageBox.Show("Some of the selected audios haven't been deleted!", "Warning");
                }
            }
        }

        private void btnCopyToNew_Click(object sender, RoutedEventArgs e)
        {
            if (lstAudios.SelectedItems.Count <= 0)
                return;

            var dialog = new TrainingDetails();
            dialog.NeedsDialogResult = true;
            dialog.CreateNew = true;
            if (dialog.ShowDialog() == true && dialog.ActiveRecord != null)
            {
                MoveSelectedAudiosToTraining(dialog.ActiveRecord.TrainingId, dialog.ActiveRecord.Title);
            }
        }

        private void btnCopyToExisting_Click(object sender, RoutedEventArgs e)
        {
            if (lstAudios.SelectedItems.Count <= 0)
                return;

            var dialog = new TrainingSelectionDialog();
            if (dialog.ShowDialog() == true && dialog.SelectedTraining != null)
            {
                MoveSelectedAudiosToTraining(dialog.SelectedTraining.TrainingId, dialog.SelectedTraining.Title);
            }
        }

        private void MoveSelectedAudiosToTraining(Guid trainingId, string trainingTitle)
        {
            bool isSuccess = _recordingProvider.MoveAudios(
                _recorderKey, new TrainingTargetKey(trainingId),
                lstAudios.SelectedItems.Cast<RecordedAudioListItem>().Select(x => x.AudioKey));
            RefreshList();
            if (isSuccess)
            {
                MessageBox.Show(string.Format(
                    "Succesfully moved the selected audios to the training '{0}'.", trainingTitle), "Success");
            }
            else
            {
                MessageBox.Show(string.Format(
                    "Some of the selected audios haven't been moved to the training '{0}'.", trainingTitle), "Warning");
            }
        }

        private void RefreshList()
        {
            lstAudios.ItemsSource = _recordingProvider.GetAudioList(_recorderKey);
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

        private void SetAudioButtonsState(bool isEnabled)
        {
            btnDeleteSelected.IsEnabled = isEnabled;
            btnCopyToExisting.IsEnabled = isEnabled;
            btnCopyToNew.IsEnabled = isEnabled;
        }
    }
}
