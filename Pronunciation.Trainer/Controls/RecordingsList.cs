using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using Pronunciation.Core;
using Pronunciation.Core.Providers.Recording;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Pronunciation.Trainer.Controls
{
    public class RecordingsList : ListBoxExt
    {
        private AudioPanel _audioPanel;

        public event EventHandler RecordingsDeleteRequested;

        public RecordingsList()
        {
            base.PreviewKeyDown += RecordingsList_PreviewKeyDown;
            base.Initialized += RecordingsList_Initialized;
        }

        public void AttachPanel(AudioPanel audioPanel)
        {
            _audioPanel = audioPanel;
        }

        private void RecordingsList_Initialized(object sender, EventArgs e)
        {
            this.Items.SortDescriptions.Add(new SortDescription("RecordingDate", ListSortDirection.Descending));
        }

        private void RecordingsList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_audioPanel == null)
                return;

            switch (e.Key)
            {
                case Key.Left:
                    _audioPanel.PlayReferenceAudio();
                    break;

                case Key.Right:
                    _audioPanel.PlayRecordedAudio();
                    break;

                case Key.Delete:
                    if (this.SelectedItems.Count > 0 && RecordingsDeleteRequested != null)
                    {
                        RecordingsDeleteRequested(this, null);
                    }
                    break;
            }
        }

        public void AttachItemsSource(IEnumerable<RecordedAudioListItem> recordings)
        {
            this.ItemsSource = new ObservableCollection<RecordedAudioListItem>(recordings);
        }

        public RecordedAudioListItem SelectedRecording
        {
            get { return this.SelectedItem as RecordedAudioListItem; }
            set { this.SelectedItem = value; }
        }

        public IEnumerable<RecordedAudioListItem> SelectedRecordings
        {
            get { return this.SelectedItems.Cast<RecordedAudioListItem>(); }
        }

        public int SelectedRecordingsCount
        {
            get { return this.SelectedItems.Count; }
        }

        public ICollection<RecordedAudioListItem> Recordings
        {
            get { return ((ICollection<RecordedAudioListItem>)this.ItemsSource); }
        }

        public void RemoveSelected()
        {
            if (this.SelectedItems.Count <= 0)
                return;

            var recordings = this.Recordings;
            foreach (var selectedItem in this.SelectedItems.Cast<RecordedAudioListItem>().ToArray())
            {
                recordings.Remove(selectedItem);
            }
        }
    }
}
