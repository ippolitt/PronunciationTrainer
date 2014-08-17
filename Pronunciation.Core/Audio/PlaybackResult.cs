using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Audio
{
    public class PlaybackResult
    {
        public TimeSpan TotalDuration { get; private set; }
        public TimeSpan PlayedDuration { get; private set; }

        public PlaybackResult(TimeSpan totalDuration)
            : this(totalDuration, totalDuration)
        {
        }

        public PlaybackResult(TimeSpan totalDuration, TimeSpan playedDuration)
        {
            TotalDuration = totalDuration;
            PlayedDuration = playedDuration;
        }
    }
}
