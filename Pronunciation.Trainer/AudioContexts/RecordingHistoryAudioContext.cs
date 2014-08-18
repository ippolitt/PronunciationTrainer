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
    public class RecordingHistoryAudioContext : IAudioContext
    {
        public event AudioContextChangedHandler ContextChanged;

        private readonly RecordingProviderWithTargetKey _recordingProvider;
        private readonly PlaybackData _referenceAudio;
        private RecordedAudioListItem _recording;

        public RecordingHistoryAudioContext(RecordingProviderWithTargetKey recordingProvider, PlaybackData referenceAudio)
        {
            _recordingProvider = recordingProvider;
            _referenceAudio = referenceAudio;
        }

        public void RefreshContext(RecordedAudioListItem recording, bool playImmediately)
        {
            _recording = recording;
            if (ContextChanged != null)
            {
                ContextChanged(playImmediately ? PlayAudioMode.PlayRecorded : PlayAudioMode.None);
            }
        }

        public void ResetContext()
        {
            _recording = null;
            if (ContextChanged != null)
            {
                ContextChanged(PlayAudioMode.None);
            }
        }

        public bool CanShowRecordingsHistory
        {
            get { return false; }
        }

        public string ContextDescription
        {
            get
            {
                return _recording == null ? null : FormatHelper.ToTimeString(_recording.Duration ?? 0, true);
            }
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

        public PlaybackData GetReferenceAudio()
        {
            return _referenceAudio;
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
