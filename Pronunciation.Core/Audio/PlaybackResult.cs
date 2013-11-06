using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace Pronunciation.Core.Audio
{
    public class PlaybackResult
    {
        private List<float> _samplesList;
        private float[] _samplesArray;

        public TimeSpan TotalDuration { get; private set; }
        public TimeSpan SamplesDuration { get; private set; }

        internal PlaybackResult(TimeSpan totalDuration)
        {
            TotalDuration = totalDuration;
            SamplesDuration = totalDuration;
        }

        internal PlaybackResult(TimeSpan totalDuration, TimeSpan samplesDuration, List<float> samples)
        {
            TotalDuration = totalDuration;
            SamplesDuration = samplesDuration;
            _samplesList = samples;
        }

        public float[] Samples
        {
            get
            {
                if (_samplesArray == null)
                {
                    _samplesArray = _samplesList.ToArray();
                    _samplesList = null;
                }

                return _samplesArray;
            }
        }
    }
}
