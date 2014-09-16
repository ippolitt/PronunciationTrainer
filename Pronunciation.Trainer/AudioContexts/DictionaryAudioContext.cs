﻿using System;
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
        private readonly IRecordingProvider<LPDTargetKey> _recordingProvider;
        private readonly IRecordingHistoryPolicy _recordingPolicy;
        private LPDTargetKey _recordingKey;
        private string _soundKey;
        private DictionarySoundInfo _soundInfo;
        private const string MWSoundPrefix = "mw_";

        public event AudioContextChangedHandler ContextChanged;

        public DictionaryAudioContext(IDictionaryProvider dictionaryProvider,
            IRecordingProvider<LPDTargetKey> recordingProvider, IRecordingHistoryPolicy recordingPolicy)
        {
            _dictionaryProvider = dictionaryProvider;
            _recordingProvider = recordingProvider;
            _recordingPolicy = recordingPolicy;
        }

        public void RefreshContext(string soundKey, bool playImmediately)
        {
            _soundKey = soundKey;
            _soundInfo = playImmediately ? _dictionaryProvider.GetAudio(soundKey) : null;
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
            if (string.IsNullOrEmpty(audioData))
            {
                _soundInfo = _dictionaryProvider.GetAudio(soundKey);
            }
            else
            {
                _soundInfo = _dictionaryProvider.GetAudioFromScriptData(soundKey, audioData);
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

                return string.Format("Active audio: \"{0}\" {1}",
                    string.IsNullOrEmpty(_soundInfo.SoundText) ? _soundKey : _soundInfo.SoundText, 
                    IsMWSound(_soundKey) ? "MW" : (_soundInfo.IsUKAudio ? "UK" : "US")); 
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

        private bool IsMWSound(string soundKey)
        {
            return !string.IsNullOrEmpty(soundKey) && soundKey.StartsWith(MWSoundPrefix);
        }
    }
}
