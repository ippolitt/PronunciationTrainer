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

        private readonly IRecordingProvider<TrainingTargetKey> _recordingProvider;
        private readonly TrainingTargetKey _trainingKey;
        private readonly IRecordingHistoryPolicy _recordingPolicy;
        private string _audioKey;

        public TrainingAudioContext(IRecordingProvider<TrainingTargetKey> recordingProvider, 
            TrainingTargetKey trainingKey, IRecordingHistoryPolicy recordingPolicy)
        {
            _recordingProvider = recordingProvider;
            _recordingPolicy = recordingPolicy;
            _trainingKey = trainingKey;
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

            return _recordingProvider.GetAudio(_trainingKey, _audioKey);
        }

        public RecordingSettings GetRecordingSettings()
        {
            return _recordingProvider.GetRecordingSettings(_trainingKey);
        }

        public string RegisterRecordedAudio(string recordedFilePath, DateTime recordingDate)
        {
            return _recordingProvider.RegisterNewAudio(_trainingKey, recordingDate, recordedFilePath, _recordingPolicy);
        }
    }
}
