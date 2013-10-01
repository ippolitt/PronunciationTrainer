using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Audio
{
    public class PlaybackResult
    {
        private List<float> _samplesList;
        private float[] _samplesArray;

        internal void CollectSamples(SamplesCollector collector)
        {
            _samplesList = collector.Samples;
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
