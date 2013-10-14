using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Contexts;
using Pronunciation.Core.Providers;
using System.IO;

namespace Pronunciation.Trainer.AudioContexts
{
    public class TrainingAudioContext : IAudioContext
    {
        private readonly TrainingProvider _provider;
        private ExerciseId _exerciseContext;
        private string _recordingName;

        public event AudioContextChangedHandler ContextChanged;

        public TrainingAudioContext(TrainingProvider provider, ExerciseId exerciseContext)
        {
            _provider = provider;
            _exerciseContext = exerciseContext;
        }

        public void ResetExerciseContect(ExerciseId exerciseContext)
        {
            _exerciseContext = exerciseContext;
            _recordingName = null;

            if (ContextChanged != null)
            {
                ContextChanged(PlayAudioMode.None);
            }
        }

        public void RefreshContext(string recordingName, bool playImmediately)
        {
            _recordingName = recordingName;

            if (ContextChanged != null)
            {
                ContextChanged(playImmediately ? PlayAudioMode.PlayReference : PlayAudioMode.None);
            }
        }

        public bool IsReferenceAudioExists
        {
            get { return !string.IsNullOrEmpty(_recordingName); }
        }

        public bool IsRecordedAudioExists
        {
            get 
            {
                if (_exerciseContext == null || string.IsNullOrEmpty(_recordingName))
                    return false;

                return File.Exists(_provider.BuildRecordedAudioPath(_exerciseContext, _recordingName)); 
            }
        }

        public bool IsRecordingAllowed
        {
            get { return IsReferenceAudioExists; }
        }

        public PlaybackSettings GetReferenceAudio()
        {
            if (_exerciseContext == null || string.IsNullOrEmpty(_recordingName))
                return null;

            return new PlaybackSettings(true, _provider.BuildReferenceAudioPath(_exerciseContext, _recordingName));
        }

        public PlaybackSettings GetRecordedAudio()
        {
            if (_exerciseContext == null || string.IsNullOrEmpty(_recordingName))
                return null;

            string audioPath = _provider.BuildRecordedAudioPath(_exerciseContext, _recordingName);
            if (!File.Exists(audioPath))
                return null;

            return new PlaybackSettings(true, audioPath);
        }

        public RecordingSettings GetRecordingSettings()
        {
            if (_exerciseContext == null || string.IsNullOrEmpty(_recordingName))
                return null;

            return new RecordingSettings(_provider.BuildRecordedAudioPath(_exerciseContext, _recordingName));
        }
    }
}
