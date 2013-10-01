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

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for Recorder.xaml
    /// </summary>
    public partial class Recorder : UserControl
    {
        private RecorderProvider _provider;
        private RecorderAudioContext _audioContext;

        public Recorder()
        {
            InitializeComponent();
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            _provider = new RecorderProvider(AppSettings.Instance.BaseFolder);
            _audioContext = new RecorderAudioContext(_provider);

            audioPanel.RecordingCompleted += AudioPanel_RecordingCompleted;
            lstRecordings.Items.SortDescriptions.Add(new SortDescription("Text", ListSortDirection.Descending));
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            audioPanel.AttachContext(_audioContext);
            lstRecordings.AttachPanel(audioPanel);

            lstRecordings.ItemsSource = _provider.GetRecordingsList();
        }

        private void AudioPanel_RecordingCompleted(string recordedFilePath)
        {
            var recordingName = _provider.GetRecordingName(recordedFilePath);
            lstRecordings.ItemsSource = _provider.GetRecordingsList().ToList();
            lstRecordings.SelectedItem = lstRecordings.Items.Cast<KeyTextPair<string>>().Single(x => x.Key == recordingName);
            lstRecordings.Focus();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            bool isSuccess = _provider.DeleteAllRecordings();
            RefreshList(isSuccess);
        }

        private void btnDeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (lstRecordings.SelectedItems.Count <= 0)
                return;

            bool isSuccess = _provider.DeleteRecordings(lstRecordings.SelectedItems.Cast<KeyTextPair<string>>().Select(x => x.Key));
            RefreshList(isSuccess);
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
    }
}
