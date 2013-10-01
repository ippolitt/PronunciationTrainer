using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace Pronunciation.Core.Audio
{
    class SamplesCollector : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private List<float> _samples;

        public SamplesCollector(ISampleProvider source)
        {
            _source = source;
            _samples = new List<float>();
        }

        public List<float> Samples
        {
            get { return _samples; }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = _source.Read(buffer, offset, count);
            for (int i = offset; i < count; i++)
            {
                _samples.Add(buffer[i]);
            }

            return samplesRead;
        }

        public WaveFormat WaveFormat
        {
            get { return _source.WaveFormat; }
        }
    }
}
