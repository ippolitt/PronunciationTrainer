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
using System.Windows.Input;

namespace Pronunciation.Trainer.AudioContexts
{
    public class DictionaryAudioContext : IAudioContext
    {
        private readonly IDictionaryProvider _dictionaryProvider;
        private readonly IRecordingProvider<DictionaryTargetKey> _recordingProvider;
        private readonly IRecordingHistoryPolicy _recordingPolicy;
        private DictionaryTargetKey _recordingKey;
        private string _soundKey;
        private string _soundText;
        private DictionarySoundInfo _soundInfo;

        public event AudioContextChangedHandler ContextChanged;

        public DictionaryAudioContext(IDictionaryProvider dictionaryProvider,
            IRecordingProvider<DictionaryTargetKey> recordingProvider, IRecordingHistoryPolicy recordingPolicy)
        {
            _dictionaryProvider = dictionaryProvider;
            _recordingProvider = recordingProvider;
            _recordingPolicy = recordingPolicy;
        }

        public void RefreshContext(string soundKey, string soundText, bool playImmediately)
        {
            _soundKey = soundKey;
            _soundText = soundText;
            _soundInfo = playImmediately ? _dictionaryProvider.GetAudio(_soundKey) : null;
            _recordingKey = string.IsNullOrEmpty(_soundKey) ? null : new DictionaryTargetKey(_soundKey);

            if (ContextChanged != null)
            {
                ContextChanged(playImmediately ? PlayAudioMode.PlayReference : PlayAudioMode.None);
            }
        }

        public void PlayScriptAudio(string soundKey, string soundText, string audioData)
        {
            if (string.IsNullOrEmpty(soundKey))
                return;

            bool isCtrlPressed = ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control);

            _soundKey = soundKey;
            _soundText = soundText;
            if (string.IsNullOrEmpty(audioData))
            {
                _soundInfo = _dictionaryProvider.GetAudio(soundKey);
            }
            else
            {
                _soundInfo = _dictionaryProvider.GetAudioFromScriptData(soundKey, audioData);
            }
            _recordingKey = new DictionaryTargetKey(soundKey);

            if (ContextChanged != null)
            {
                ContextChanged(isCtrlPressed ? PlayAudioMode.PlayRecorded : PlayAudioMode.PlayReference);
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

            return new RecordingProviderWithTargetKey<DictionaryTargetKey>(
                _recordingProvider, _recordingKey, new AlwaysAddRecordingPolicy()); 
        }

        public bool IsReferenceAudioExists
        {
            get { return !string.IsNullOrEmpty(_soundKey); }
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
                if (_soundInfo == null)
                    return null;

                return string.Format("Active audio: \"{0}\" {1} ({2})",
                    _soundText, 
                    _soundInfo.IsUKAudio ? "UK" : "US",
                    _soundKey); 
            }
        }

        public string CurrentSoundKey
        {
            get { return _soundKey; }
        }

        public PlaybackData GetReferenceAudio()
        {
            if (string.IsNullOrEmpty(_soundKey))
                return null;

            if (_soundInfo != null)
                return _soundInfo.Data;

            _soundInfo = _dictionaryProvider.GetAudio(_soundKey);
            return _soundInfo == null ? null : _soundInfo.Data;
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
