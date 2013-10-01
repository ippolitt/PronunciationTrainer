using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using Pronunciation.Core;

namespace Pronunciation.Trainer
{
    public class AudioList : ListBox
    {
        private bool _isRecording;
        private AudioPanel _audioPanel;

        public AudioList()
        {
            base.PreviewKeyDown += AudioList_PreviewKeyDown;
            base.PreviewKeyUp += AudioList_PreviewKeyUp;
        }

        public void AttachPanel(AudioPanel audioPanel)
        {
            _audioPanel = audioPanel;
        }

        private void AudioList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_audioPanel == null)
                return;

            switch (e.Key)
            {
                case Key.Left:
                case Key.Z:
                    _audioPanel.PlayReferenceAudio();
                    break;
                case Key.Right:
                case Key.C:
                    _audioPanel.PlayRecordedAudio();
                    break;
                case Key.X:
                    if (!_isRecording)
                    {
                        _isRecording = true;
                        _audioPanel.StartRecording();
                    }
                    break;
            }
        }

        private void AudioList_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (_audioPanel == null)
                return;

            switch (e.Key)
            {
                case Key.X:
                    if (_isRecording)
                    {
                        _isRecording = false;
                        _audioPanel.StopAction(true);
                    }
                    break;

                case Key.Space:
                    _audioPanel.StopAction(true);
                    break;
            }
        }
    }
}
