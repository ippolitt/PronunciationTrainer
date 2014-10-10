using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Providers.Recording;

namespace Pronunciation.Core.Contexts
{
    public delegate void AudioContextChangedHandler(PlayAudioMode playMode);

    public enum PlayAudioMode
    {
        None,
        PlayReference,
        PlayRecorded
    }

    public interface IAudioContext
    {
        bool IsReferenceAudioExists { get; }
        bool IsRecordedAudioExists { get; }
        bool IsRecordingAllowed { get; }
        PlaybackData GetReferenceAudio();
        PlaybackData GetRecordedAudio();
        RecordingSettings GetRecordingSettings();

        bool CanShowRecordingsHistory { get; }
        RecordingProviderWithTargetKey GetRecordingHistoryProvider();

        string ContextDescription { get; }
        event AudioContextChangedHandler ContextChanged;

        bool SuportsFavoriteAudio { get; }
        bool? IsFavoriteAudio { get; set; }
    }
}
