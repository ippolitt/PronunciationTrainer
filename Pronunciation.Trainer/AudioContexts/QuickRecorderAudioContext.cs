using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Contexts;
using Pronunciation.Core.Providers.Training;
using Pronunciation.Core.Providers.Recording;

namespace Pronunciation.Trainer.AudioContexts
{
    public class QuickRecorderAudioContext : IAudioContext
    {
        public event AudioContextChangedHandler ContextChanged;

        private readonly IRecordingProvider<QuickRecorderTargetKey> _recordingProvider;
        private readonly QuickRecorderTargetKey _recorderKey;
        private readonly IRecordingHistoryPolicy _recordingPolicy;
        private string _audioKey;

        public QuickRecorderAudioContext(IRecordingProvider<QuickRecorderTargetKey> recordingProvider, 
            QuickRecorderTargetKey recorderKey, IRecordingHistoryPolicy recordingPolicy)
        {
            _recordingProvider = recordingProvider;
            _recordingPolicy = recordingPolicy;
            _recorderKey = recorderKey;
        }

        public void RefreshContext(string audioKey, bool playImmediately)
        {
            if (_audioKey == audioKey && !playImmediately)
                return;

            _audioKey = audioKey;
            if (ContextChanged != null)
            {
                ContextChanged(playImmediately ? PlayAudioMode.PlayRecorded : PlayAudioMode.None);
            }
        }

        public void ResetContext()
        {
            _audioKey = null;
            if (ContextChanged != null)
            {
                ContextChanged(PlayAudioMode.None);
            }
        }

        public bool IsReferenceAudioExists
        {
            get { return false; }
        }

        public bool IsRecordedAudioExists
        {
            get { return !string.IsNullOrEmpty(_audioKey); }
        }

        public bool IsRecordingAllowed
        {
            get { return true; }
        }

        public PlaybackData GetReferenceAudio()
        {
            return null;
        }

        public PlaybackData GetRecordedAudio()
        {
            if (string.IsNullOrEmpty(_audioKey))
                return null;

            return _recordingProvider.GetAudio(_recorderKey, _audioKey);
        }

        public RecordingSettings GetRecordingSettings()
        {
            return _recordingProvider.GetRecordingSettings(_recorderKey);
        }

        public string RegisterRecordedAudio(string recordedFilePath, DateTime recordingDate)
        {
            return _recordingProvider.RegisterNewAudio(_recorderKey, recordingDate, recordedFilePath, _recordingPolicy);
        }
    }
}
