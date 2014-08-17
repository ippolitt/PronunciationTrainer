using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Audio
{
    public class AudioInfo
    {
        public TimeSpan Duration { get; private set; }
        public int SamplesCount { get; private set; }
        public int SampleRate { get; private set; }

        public AudioInfo(TimeSpan duration, int samplesCount, int sampleRate)
        {
            Duration = duration;
            SamplesCount = samplesCount;
            SampleRate = sampleRate;
        }
    }
}
