using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Contexts;
using Pronunciation.Core.Providers;
using System.IO;
using System.Windows.Controls;

namespace Pronunciation.Trainer.AudioContexts
{
    public class DictionaryAudioContext : IAudioContext
    {
        private readonly IDictionaryProvider _provider;
        private string _currentAudioKey;
        private PlaybackSettings _referenceAudio;

        public event AudioContextChangedHandler ContextChanged;

        public DictionaryAudioContext(IDictionaryProvider provider)
        {
            _provider = provider;
        }

        public void RefreshContext(IndexEntry currentIndex, bool useUkAudio, bool playImmediately)
        {
            _currentAudioKey = null;
            _referenceAudio = null;
            if (currentIndex != null)
            {
                _currentAudioKey = useUkAudio ? currentIndex.SoundKeyUK : currentIndex.SoundKeyUS;
            }

            if (ContextChanged != null)
            {
                ContextChanged(playImmediately ? PlayAudioMode.PlayReference : PlayAudioMode.None);
            }
        }

        public void PlayScriptAudio(string audioKey, string audioData)
        {
            if (string.IsNullOrEmpty(audioKey))
                return;

            _currentAudioKey = audioKey;
            _referenceAudio = null;
            if (!string.IsNullOrEmpty(audioData))
            {
                _referenceAudio = _provider.GetAudioFromScriptData(audioData);
            }

            if (ContextChanged != null)
            {
                ContextChanged(PlayAudioMode.PlayReference);
            }
        }

        public bool IsReferenceAudioExists
        {
            get { return !string.IsNullOrEmpty(_currentAudioKey); }
        }

        public bool IsRecordedAudioExists
        {
            get
            {
                if (string.IsNullOrEmpty(_currentAudioKey))
                    return false;

                return _provider.IsRecordedAudioExists(_currentAudioKey);
            }
        }

        public bool IsRecordingAllowed
        {
            get { return IsReferenceAudioExists; }
        }

        public PlaybackSettings GetReferenceAudio()
        {
            if (string.IsNullOrEmpty(_currentAudioKey))
                return null;

            if (_referenceAudio == null)
            {
                _referenceAudio = _provider.GetReferenceAudio(_currentAudioKey);
            }

            return _referenceAudio;
        }

        public PlaybackSettings GetRecordedAudio()
        {
            if (string.IsNullOrEmpty(_currentAudioKey))
                return null;

            return _provider.GetRecordedAudio(_currentAudioKey);
        }

        public RecordingSettings GetRecordingSettings()
        {
            if (string.IsNullOrEmpty(_currentAudioKey))
                return null;

            return _provider.GetRecordingSettings(_currentAudioKey);
        }
    }
}
