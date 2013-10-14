using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        PlaybackSettings GetReferenceAudio();
        PlaybackSettings GetRecordedAudio();
        RecordingSettings GetRecordingSettings();
        event AudioContextChangedHandler ContextChanged;
    }
}
