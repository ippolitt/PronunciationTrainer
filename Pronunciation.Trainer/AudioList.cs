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
        private AudioPanel _audioPanel;

        public AudioList()
        {
            base.PreviewKeyDown += AudioList_PreviewKeyDown;
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
                    _audioPanel.PlayReferenceAudio();
                    break;
                case Key.Right:
                    _audioPanel.PlayRecordedAudio();
                    break;
            }
        }
    }
}
