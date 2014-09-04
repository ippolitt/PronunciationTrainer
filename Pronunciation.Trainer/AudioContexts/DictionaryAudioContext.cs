using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Contexts;
using Pronunciation.Core.Providers.Dictionary;
using System.IO;
using System.Windows.Controls;
using Pronunciation.Core.Providers.Recording;
using Pronunciation.Core.Providers.Recording.HistoryPolicies;
using Pronunciation.Trainer.Dictionary;

namespace Pronunciation.Trainer.AudioContexts
{
    public class DictionaryAudioContext : IAudioContext
    {
        private readonly IDictionaryProvider _dictionaryProvider;
        private readonly DictionaryIndex _dictionaryIndex;
        private readonly IRecordingProvider<LPDTargetKey> _recordingProvider;
        private readonly IRecordingHistoryPolicy _recordingPolicy;
        private LPDTargetKey _recordingKey;
        private PlaybackData _referenceAudio;
        private IndexEntry _soundEntry;
        private string _soundKey;
        private bool _isUKSound;

        public event AudioContextChangedHandler ContextChanged;

        public DictionaryAudioContext(IDictionaryProvider dictionaryProvider, DictionaryIndex dictionaryIndex,
            IRecordingProvider<LPDTargetKey> recordingProvider, IRecordingHistoryPolicy recordingPolicy)
        {
            _dictionaryProvider = dictionaryProvider;
            _dictionaryIndex = dictionaryIndex;
            _recordingProvider = recordingProvider;
            _recordingPolicy = recordingPolicy;
        }

        public void RefreshContext(IndexEntry soundEntry, bool isUKSound, bool playImmediately)
        {
            _soundEntry = soundEntry;
            _isUKSound = isUKSound;
            _soundKey = isUKSound ? soundEntry.SoundKeyUK : soundEntry.SoundKeyUS;
            _referenceAudio = null;
            _recordingKey = string.IsNullOrEmpty(_soundKey) ? null : new LPDTargetKey(_soundKey);

            if (ContextChanged != null)
            {
                ContextChanged(playImmediately ? PlayAudioMode.PlayReference : PlayAudioMode.None);
            }
        }

        public void PlayScriptAudio(string soundKey, string audioData)
        {
            if (string.IsNullOrEmpty(soundKey))
                return;

            _soundKey = soundKey;
            _soundEntry = _dictionaryIndex.GetEntryBySoundKey(soundKey);
            if (_soundEntry != null)
            {
                _isUKSound = string.Equals(soundKey, _soundEntry.SoundKeyUK, StringComparison.OrdinalIgnoreCase);
            }

            _referenceAudio = null;
            if (!string.IsNullOrEmpty(audioData))
            {
                _referenceAudio = _dictionaryProvider.GetAudioFromScriptData(audioData);
            }

            _recordingKey = new LPDTargetKey(soundKey);

            if (ContextChanged != null)
            {
                ContextChanged(PlayAudioMode.PlayReference);
            }
        }

        public bool CanShowRecordingsHistory
        {
            get { return _recordingKey != null; }
        }

        public RecordingProviderWithTargetKey GetRecordingHistoryProvider()
        {
            if (_recordingKey == null)
                throw new InvalidOperationException();

            return new RecordingProviderWithTargetKey<LPDTargetKey>(
                _recordingProvider, _recordingKey, new AlwaysAddRecordingPolicy()); 
        }

        public bool IsReferenceAudioExists
        {
            get { return _referenceAudio != null || !string.IsNullOrEmpty(_soundKey); }
        }

        public bool IsRecordedAudioExists
        {
            get
            {
                return _recordingKey == null ? false : _recordingProvider.ContainsAudios(_recordingKey);
            }
        }

        public bool IsRecordingAllowed
        {
            get { return IsReferenceAudioExists; }
        }

        public string ContextDescription
        {
            get 
            { 
                return _soundEntry == null
                    ? (string.IsNullOrEmpty(_soundKey) ? null : string.Format("Active audio: \"{0}\"", _soundKey))
                    : string.Format("Active audio: \"{0}\" {1}", _soundEntry.EntryText, _isUKSound ? "UK" : "US"); 
            }
        }

        public string CurrentSoundName
        {
            get { return _soundEntry == null ? _soundKey : _soundEntry.EntryText; }
        }

        public PlaybackData GetReferenceAudio()
        {
            if (_referenceAudio != null)
                return _referenceAudio;

            if (!string.IsNullOrEmpty(_soundKey))
            {
                _referenceAudio = _dictionaryProvider.GetAudio(_soundKey);
            }

            return _referenceAudio;
        }

        public PlaybackData GetRecordedAudio()
        {
            return _recordingKey == null ? null : _recordingProvider.GetLatestAudio(_recordingKey);
        }

        public RecordingSettings GetRecordingSettings()
        {
            return _recordingKey == null ? null : _recordingProvider.GetRecordingSettings(_recordingKey);
        }

        public string RegisterRecordedAudio(string recordedFilePath, DateTime recordingDate)
        {
            return _recordingProvider.RegisterNewAudio(_recordingKey, recordingDate, recordedFilePath, _recordingPolicy);
        }
    }
}
