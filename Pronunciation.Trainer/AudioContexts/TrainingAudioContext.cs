using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Contexts;
using System.IO;
using Pronunciation.Core.Providers.Recording;
using Pronunciation.Core.Providers.Training;
using Pronunciation.Trainer.Utility;

namespace Pronunciation.Trainer.AudioContexts
{
    public class TrainingAudioContext : IAudioContext
    {
        public event AudioContextChangedHandler ContextChanged;

        private readonly RecordingProviderWithTargetKey _recordingProvider;
        private RecordedAudioListItem _recording;
        private byte[] _referenceAudio;

        public TrainingAudioContext(RecordingProviderWithTargetKey recordingProvider)
        {
            _recordingProvider = recordingProvider;
        }

        public void RefreshContext(byte[] referenceAudio, RecordedAudioListItem recording, bool playImmediately)
        {
            _referenceAudio = referenceAudio;
            _recording = recording;
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
            get { return _recording != null; }
        }

        public bool IsRecordingAllowed
        {
            get { return true; }
        }

        public string ContextDescription
        {
            get
            {
                return _recording == null
                    ? null
                    : string.Format("Active recording: \"{0}\", duration: {1}",
                        _recording.Text, FormatHelper.ToTimeString(_recording.Duration ?? 0, true)); 
            }
        }

        public PlaybackData GetReferenceAudio()
        {
            return _referenceAudio == null ? null : new PlaybackData(_referenceAudio);
        }

        public PlaybackData GetRecordedAudio()
        {
            return _recording == null ? null : _recordingProvider.GetAudio(_recording.AudioKey);
        }

        public RecordingSettings GetRecordingSettings()
        {
            return _recordingProvider.GetRecordingSettings();
        }
    }
}
