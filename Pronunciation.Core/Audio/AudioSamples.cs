using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Audio
{
    public class AudioSamples
    {
        public TimeSpan SamplesDuration { get; private set; }
        public TimeSpan? TotalAudioDuration { get; private set; }
        public float[] LeftChannel { get; private set; }
        public float[] RightChannel { get; private set; }

        public AudioSamples(TimeSpan samplesDuration, TimeSpan? totalAudioDuration, float[] monoSamples)
        {
            SamplesDuration = samplesDuration;
            TotalAudioDuration = totalAudioDuration;
            LeftChannel = monoSamples;
        }

        public AudioSamples(TimeSpan samplesDuration, TimeSpan? totalAudioDuration, float[] leftChannel, float[] rightChannel)
            : this(samplesDuration, totalAudioDuration, leftChannel) 
        {
            if (leftChannel.Length != rightChannel.Length)
                throw new ArgumentException();

            RightChannel = rightChannel;
        }

        public bool IsStereo
        {
            get { return RightChannel != null; }
        }
    }
}
