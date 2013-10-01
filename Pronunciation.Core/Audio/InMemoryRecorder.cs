using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;
using System.IO;
using System.Threading;

namespace Pronunciation.Core.Audio
{
    internal class InMemoryRecorder
    {
        private IWaveIn _waveIn;
        private MemoryStream _memoryWriter;
        private Exception _lastError;

        public InMemoryRecorder(int sampleRate)
        {
            _waveIn = new WaveInEvent();
            _waveIn.WaveFormat = new WaveFormat(sampleRate, 1);
            _waveIn.DataAvailable += new EventHandler<WaveInEventArgs>(_waveIn_DataAvailable);
            _waveIn.RecordingStopped += new EventHandler<StoppedEventArgs>(_waveIn_RecordingStopped);

            _memoryWriter = new MemoryStream();
        }

        public WaveFormat DataFormat
        {
            get { return (_waveIn == null ? null : _waveIn.WaveFormat); }
        }

        public void Start()
        {
            if (_waveIn == null)
                throw new InvalidOperationException("The recorder has already been disposed!");

            _waveIn.StartRecording();
        }

        public byte[] Stop()
        {
            if (_waveIn == null)
                throw new InvalidOperationException("The recorder has already been disposed!");

            try
            {
                _waveIn.StopRecording();
                if (_lastError != null)
                    throw _lastError;

                return _memoryWriter.ToArray();
            }
            finally
            {
                _waveIn.Dispose();
                _memoryWriter.Dispose();
                _waveIn = null;
                _memoryWriter = null;
            }
        }

        private void _waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (_memoryWriter != null)
            {
                _memoryWriter.Write(e.Buffer, 0, e.BytesRecorded);
            }
        }

        private void _waveIn_RecordingStopped(object sender, StoppedEventArgs e)
        {
            _lastError = e.Exception;
        }
    }
}
