using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Contexts;
using Pronunciation.Core.Providers;
using System.IO;

namespace Pronunciation.Trainer.AudioContexts
{
    public class RecordingAudioContext : IAudioContext
    {
        public event AudioContextChangedHandler ContextChanged;

        private readonly RecordingProvider _provider;
        private readonly Guid _recordingId;
        private string _recordingName;

        public RecordingAudioContext(RecordingProvider provider, Guid recordingId)
        {
            _provider = provider;
            _recordingId = recordingId;
        }

        public void RefreshContext(string recordingName, bool playImmediately)
        {
            _recordingName = recordingName;

            if (ContextChanged != null)
            {
                ContextChanged(playImmediately ? PlayAudioMode.PlayRecorded : PlayAudioMode.None);
            }
        }

        public string RecordingName
        {
            get { return _recordingName; }
        }

        public bool IsReferenceAudioExists
        {
            get { return false; }
        }

        public bool IsRecordedAudioExists
        {
            get 
            {
                if (string.IsNullOrEmpty(_recordingName))
                    return false;

                var recordingPath = _provider.BuildAudioFilePath(_recordingId, _recordingName);
                return File.Exists(recordingPath);
            }
        }

        public bool IsRecordingAllowed
        {
            get { return true; }
        }

        public PlaybackSettings GetReferenceAudio()
        {
            return null;
        }

        public PlaybackSettings GetRecordedAudio()
        {
            if (string.IsNullOrEmpty(_recordingName))
                return null;

            var recordingPath = _provider.BuildAudioFilePath(_recordingId, _recordingName);
            if (!File.Exists(recordingPath))
                return null;

            return new PlaybackSettings(recordingPath);
        }

        public RecordingSettings GetRecordingSettings()
        {
            var newRecordingName = _provider.BuildNewAudioName();
            return new RecordingSettings(_provider.BuildAudioFilePath(_recordingId, newRecordingName));
        }
    }
}
