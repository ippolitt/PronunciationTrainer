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
using Pronunciation.Core.Providers;
using System.ComponentModel;
using Pronunciation.Core;
using System.IO;

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for Recorder.xaml
    /// </summary>
    public partial class Recorder : UserControlExt, ISupportsKeyboardFocus
    {
        private QuickRecorderProvider _provider;
        private QuickRecorderAudioContext _audioContext;

        public Recorder()
        {
            InitializeComponent();
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            _provider = new QuickRecorderProvider(AppSettings.Instance.Folders.QuickRecorder);
            _audioContext = new QuickRecorderAudioContext(_provider);

            audioPanel.RecordingCompleted += AudioPanel_RecordingCompleted;
            lstRecordings.Items.SortDescriptions.Add(new SortDescription("Text", ListSortDirection.Descending));

            audioPanel.AttachContext(_audioContext);
            lstRecordings.AttachPanel(audioPanel);

            lstRecordings.ItemsSource = _provider.GetRecordingsList();
            if (lstRecordings.Items.Count > 0)
            {
                lstRecordings.SelectedIndex = 0;
                lstRecordings.Focus();
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }

        public void CaptureKeyboardFocus()
        {
            lstRecordings.Focus();
        }

        private void AudioPanel_RecordingCompleted(string recordedFilePath)
        {
            var recordingName = _provider.GetRecordingName(recordedFilePath);
            lstRecordings.ItemsSource = _provider.GetRecordingsList().ToList();
            lstRecordings.SelectedItem = lstRecordings.Items.Cast<KeyTextPair<string>>().Single(x => x.Key == recordingName);
            lstRecordings.Focus();
        }

        private void RefreshList(bool isSuccess)
        {
            lstRecordings.ItemsSource = _provider.GetRecordingsList().ToList();
            if (lstRecordings.Items.Count > 0)
            {
                lstRecordings.SelectedIndex = 0;
                lstRecordings.Focus();
            }
            else
            {
                _audioContext.RefreshContext(null, false);
            }

            if (!isSuccess)
            {
                MessageBox.Show("Some of the files are currently in use and can't be deleted!", "Error");
            }
        }

        private void lstRecordings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = lstRecordings.SelectedItem as KeyTextPair<string>;
            if (selectedItem == null)
                return;

            _audioContext.RefreshContext(selectedItem.Key, false);
        }

        private void lstRecordings_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedItem = lstRecordings.SelectedItem as KeyTextPair<string>;
            if (selectedItem == null)
                return;

            _audioContext.RefreshContext(selectedItem.Key, true);
        }

        private void btnDeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (lstRecordings.SelectedItems.Count <= 0)
                return;

            var result = MessageBox.Show(
                "Are you sure that you want to delete the selected records?",
                "Confirm deletion", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                bool isSuccess = _provider.DeleteRecordings(lstRecordings.SelectedItems.Cast<KeyTextPair<string>>().Select(x => x.Key));
                RefreshList(isSuccess);
            }
        }

        private void btnCopyToNew_Click(object sender, RoutedEventArgs e)
        {
            if (lstRecordings.SelectedItems.Count <= 0)
                return;

            var files = lstRecordings.SelectedItems.Cast<KeyTextPair<string>>()
                .Select(x => _provider.BuildRecordingPath(x.Key)).ToList();

            var dialog = new RecordingDetails();
            dialog.NeedsDialogResult = true;
            dialog.CreateNew = true;
            if (dialog.ShowDialog() == true && dialog.ActiveRecord != null)
            {
                CopyFilesToRecording(dialog.ActiveRecord.RecordingId, files);
                MessageBox.Show(string.Format(
                    "Succesfully copied the selected files to the newly created recording '{0}'.",
                    dialog.ActiveRecord.Title), "Success");
            }
        }

        private void btnCopyToExisting_Click(object sender, RoutedEventArgs e)
        {
            if (lstRecordings.SelectedItems.Count <= 0)
                return;

            var files = lstRecordings.SelectedItems.Cast<KeyTextPair<string>>()
                .Select(x => _provider.BuildRecordingPath(x.Key)).ToList();

            var dialog = new RecordingSelectionDialog();
            if (dialog.ShowDialog() == true && dialog.SelectedRecording != null)
            {
                CopyFilesToRecording(dialog.SelectedRecording.RecordingId, files);
                MessageBox.Show(string.Format(
                    "Succesfully copied the selected files to the existing recording '{0}'.", 
                    dialog.SelectedRecording.Title), "Success");
            }
        }

        private void CopyFilesToRecording(Guid recordingId, IEnumerable<string> sourceFiles)
        {
            var provider = new RecordingProvider(AppSettings.Instance.Folders.Recordings);
            foreach (var sourceFile in sourceFiles)
            {
                string targetFile = provider.BuildAudioFilePath(recordingId, provider.GetAudioName(sourceFile));
                string targetFolder = System.IO.Path.GetDirectoryName(targetFile);
                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }
                File.Copy(sourceFile, targetFile, false);
            }
        }
    }
}
