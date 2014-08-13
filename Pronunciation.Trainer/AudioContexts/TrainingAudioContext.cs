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
    public class TrainingAudioContext : IAudioContext
    {
        public event AudioContextChangedHandler ContextChanged;

        private readonly RecordingProviderWithTargetKey _recordingProvider;
        private string _audioKey;
        private byte[] _referenceAudio;

        public TrainingAudioContext(RecordingProviderWithTargetKey recordingProvider)
        {
            _recordingProvider = recordingProvider;
        }

        public void RefreshContext(byte[] referenceAudio, string audioKey, bool playImmediately)
        {
            _referenceAudio = referenceAudio;
            _audioKey = audioKey;
            if (ContextChanged != null)
            {
                ContextChanged(playImmediately ? PlayAudioMode.PlayRecorded : PlayAudioMode.None);
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
            return _referenceAudio == null ? null : new PlaybackData(_referenceAudio);
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
