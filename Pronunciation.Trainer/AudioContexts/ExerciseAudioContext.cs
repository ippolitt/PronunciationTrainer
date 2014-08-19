using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Contexts;
using Pronunciation.Core.Providers.Exercise;
using System.IO;
using Pronunciation.Core.Providers.Recording;
using Pronunciation.Core.Providers.Recording.HistoryPolicies;
using Pronunciation.Trainer.Views;
using Pronunciation.Trainer.Utility;

namespace Pronunciation.Trainer.AudioContexts
{
    public class ExerciseAudioContext : IAudioContext
    {
        private readonly IRecordingProvider<ExerciseTargetKey> _recordingProvider;
        private readonly IRecordingHistoryPolicy _recordingPolicy;
        private readonly Guid _exerciseId;
        private ExerciseAudioListItemWithData _exerciseAudio;
        private ExerciseTargetKey _recordingKey;

        public event AudioContextChangedHandler ContextChanged;

        public ExerciseAudioContext(IRecordingProvider<ExerciseTargetKey> recordingProvider, Guid exerciseId,
            IRecordingHistoryPolicy recordingPolicy)
        {
            _recordingProvider = recordingProvider;
            _exerciseId = exerciseId;
            _recordingPolicy = recordingPolicy;
        }

        public void RefreshContext(ExerciseAudioListItemWithData exerciseAudio, bool playImmediately)
        {
            _exerciseAudio = exerciseAudio;
            _recordingKey = new ExerciseTargetKey(_exerciseId, exerciseAudio.AudioName);
            if (ContextChanged != null)
            {
                ContextChanged(playImmediately ? PlayAudioMode.PlayReference : PlayAudioMode.None);
            }
        }

        public void ResetContext()
        {
            _exerciseAudio = null;
            _recordingKey = null;
            if (ContextChanged != null)
            {
                ContextChanged(PlayAudioMode.None);
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

            return new RecordingProviderWithTargetKey<ExerciseTargetKey>(
                _recordingProvider, _recordingKey, new AlwaysAddRecordingPolicy()); 
        }

        public bool IsReferenceAudioExists
        {
            get 
            { 
                return (_exerciseAudio != null && _exerciseAudio.RawData != null); 
            }
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
            get { return _recordingKey != null; }
        }

        public string ContextDescription
        {
            get 
            {
                return _exerciseAudio == null ? null : string.Format("Active audio: \"{0}\"", _exerciseAudio.AudioName); 
            }
        }

        public PlaybackData GetReferenceAudio()
        {
            return (_exerciseAudio == null || _exerciseAudio.RawData == null) 
                ? null : new PlaybackData(_exerciseAudio.RawData);
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
