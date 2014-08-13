using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Contexts;
using System.IO;
using Pronunciation.Core.Providers.Recording;
using Pronunciation.Core.Providers.Training;

namespace Pronunciation.Trainer.AudioContexts
{
    public class RecordingHistoryAudioContext : IAudioContext
    {
        public event AudioContextChangedHandler ContextChanged;

        private readonly RecordingProviderWithTargetKey _recordingProvider;
        private readonly PlaybackData _referenceAudio;
        private string _audioKey;

        public RecordingHistoryAudioContext(RecordingProviderWithTargetKey recordingProvider, PlaybackData referenceAudio)
        {
            _recordingProvider = recordingProvider;
            _referenceAudio = referenceAudio;
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

        public bool CanShowRecordingsHistory
        {
            get { return false; }
        }

        public RecordingProviderWithTargetKey GetRecordingHistoryProvider()
        {
            throw new NotSupportedException();
        }

        public bool IsReferenceAudioExists
        {
            get { return _referenceAudio != null; }
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
            return _referenceAudio;
        }

        public PlaybackData GetRecordedAudio()
        {
            if (string.IsNullOrEmpty(_audioKey))
                return null;

            return _recordingProvider.GetAudio(_audioKey);
        }

        public RecordingSettings GetRecordingSettings()
        {
            return _recordingProvider.GetRecordingSettings();
        }
    }
}
