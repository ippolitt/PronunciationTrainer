using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Contexts;
using Pronunciation.Core.Providers.Exercise;
using System.IO;
using Pronunciation.Core.Providers.Recording;
using Pronunciation.Core.Providers.Recording.HistoryPolicies;

namespace Pronunciation.Trainer.AudioContexts
{
    public class ExerciseAudioContext : IAudioContext
    {
        private readonly IRecordingProvider<ExerciseTargetKey> _recordingProvider;
        private readonly IRecordingHistoryPolicy _recordingPolicy;
        private readonly Guid _exerciseId;
        private ExerciseTargetKey _targetKey;
        private byte[] _referenceAudio;

        public event AudioContextChangedHandler ContextChanged;

        public ExerciseAudioContext(IRecordingProvider<ExerciseTargetKey> recordingProvider, Guid exerciseId,
            IRecordingHistoryPolicy recordingPolicy)
        {
            _recordingProvider = recordingProvider;
            _exerciseId = exerciseId;
            _recordingPolicy = recordingPolicy;
        }

        public void RefreshContext(string audioName, byte[] referenceAudio, bool playImmediately)
        {
            _targetKey = new ExerciseTargetKey(_exerciseId, audioName);
            _referenceAudio = referenceAudio;
            if (ContextChanged != null)
            {
                ContextChanged(playImmediately ? PlayAudioMode.PlayReference : PlayAudioMode.None);
            }
        }

        public void ResetContext()
        {
            _targetKey = null;
            _referenceAudio = null;

            if (ContextChanged != null)
            {
                ContextChanged(PlayAudioMode.None);
            }
        }

        public bool CanShowRecordingsHistory
        {
            get { return _targetKey != null; }
        }

        public RecordingProviderWithTargetKey GetRecordingHistoryProvider()
        {
            if (_targetKey == null)
                throw new InvalidOperationException();

            return new RecordingProviderWithTargetKey<ExerciseTargetKey>(
                _recordingProvider, _targetKey, new AlwaysAddRecordingPolicy()); 
        }

        public bool IsReferenceAudioExists
        {
            get { return _referenceAudio != null; }
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
            get { return _targetKey != null; }
        }

        public PlaybackData GetReferenceAudio()
        {
            return _referenceAudio == null ? null : new PlaybackData(_referenceAudio);
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
