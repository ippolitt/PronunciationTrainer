using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Contexts;
using Pronunciation.Core.Providers.Training;
using Pronunciation.Core.Providers.Recording;
using Pronunciation.Trainer.Utility;

namespace Pronunciation.Trainer.AudioContexts
{
    public class QuickRecorderAudioContext : IAudioContext
    {
        public event AudioContextChangedHandler ContextChanged;

        private readonly RecordingProviderWithTargetKey _recordingProvider;
        private RecordedAudioListItem _recording;

        public QuickRecorderAudioContext(RecordingProviderWithTargetKey recordingProvider)
        {
            _recordingProvider = recordingProvider;
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

        public RecordingProviderWithTargetKey GetRecordingHistoryProvider()
        {
            throw new NotSupportedException();
        }

        public bool IsReferenceAudioExists
        {
            get { return false; }
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
            return null;
        }

        public PlaybackData GetRecordedAudio()
        {
            return _recording == null ? null : _recordingProvider.GetAudio(_recording.AudioKey);
        }

        public RecordingSettings GetRecordingSettings()
        {
            return _recordingProvider.GetRecordingSettings();
        }

        public bool SuportsFavoriteAudio
        {
            get { return false; }
        }

        public bool? IsFavoriteAudio
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }
}
