using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Contexts;
using Pronunciation.Core.Providers.Dictionary;
using System.IO;
using System.Windows.Controls;
using Pronunciation.Core.Providers.Recording;

namespace Pronunciation.Trainer.AudioContexts
{
    public class DictionaryAudioContext : IAudioContext
    {
        private readonly IDictionaryProvider _dictionaryProvider;
        private readonly IRecordingProvider<LPDTargetKey> _recordingProvider;
        private readonly IRecordingHistoryPolicy _recordingPolicy;
        private LPDTargetKey _targetKey;
        private PlaybackData _referenceAudio;

        public event AudioContextChangedHandler ContextChanged;

        public DictionaryAudioContext(IDictionaryProvider dictionaryProvider,
            IRecordingProvider<LPDTargetKey> recordingProvider, IRecordingHistoryPolicy recordingPolicy)
        {
            _dictionaryProvider = dictionaryProvider;
            _recordingProvider = recordingProvider;
            _recordingPolicy = recordingPolicy;
        }

        public void RefreshContext(IndexEntry currentIndex, bool useUkAudio, bool playImmediately)
        {
            _targetKey = null;
            _referenceAudio = null;
            if (currentIndex != null)
            {
                _targetKey = new LPDTargetKey(useUkAudio ? currentIndex.SoundKeyUK : currentIndex.SoundKeyUS);
            }

            if (ContextChanged != null)
            {
                ContextChanged(playImmediately ? PlayAudioMode.PlayReference : PlayAudioMode.None);
            }
        }

        public void PlayScriptAudio(string soundKey, string audioData)
        {
            if (string.IsNullOrEmpty(soundKey))
                return;

            _targetKey = new LPDTargetKey(soundKey);
            _referenceAudio = null;
            if (!string.IsNullOrEmpty(audioData))
            {
                _referenceAudio = _dictionaryProvider.GetAudioFromScriptData(audioData);
            }

            if (ContextChanged != null)
            {
                ContextChanged(PlayAudioMode.PlayReference);
            }
        }

        public bool IsReferenceAudioExists
        {
            get { return _targetKey != null; }
        }

        public bool IsRecordedAudioExists
        {
            get
            {
                return _targetKey == null ? false : _recordingProvider.ContainsAudios(_targetKey);
            }
        }

        public bool IsRecordingAllowed
        {
            get { return IsReferenceAudioExists; }
        }

        public PlaybackData GetReferenceAudio()
        {
            if (_targetKey == null)
                return null;

            if (_referenceAudio == null)
            {
                _referenceAudio = _dictionaryProvider.GetAudio(_targetKey.SoundKey);
            }

            return _referenceAudio;
        }

        public PlaybackData GetRecordedAudio()
        {
            return _targetKey == null ? null : _recordingProvider.GetLatestAudio(_targetKey);
        }

        public RecordingSettings GetRecordingSettings()
        {
            return _targetKey == null ? null : _recordingProvider.GetRecordingSettings(_targetKey);
        }

        public string RegisterRecordedAudio(string recordedFilePath, DateTime recordingDate)
        {
            return _recordingProvider.RegisterNewAudio(_targetKey, recordingDate, recordedFilePath, _recordingPolicy);
        }
    }
}
