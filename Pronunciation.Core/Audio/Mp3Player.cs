using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NAudio.Wave;
using System.IO;
using NAudio.Wave.SampleProviders;

namespace Pronunciation.Core.Audio
{
    public class Mp3Player
    {
        private AutoResetEvent _stopWait;
        private Exception _lastError;
        private IWavePlayer _wavePlayer;
        private Mp3FileReader _mp3Reader;

        private readonly bool _collectSamples;
        private readonly object _syncLock = new object();
        private readonly object _playLock = new object();

        public Mp3Player(bool collectSamples)
        {
            _collectSamples = collectSamples;
            _stopWait = new AutoResetEvent(false);
        }

        public TimeSpan CurrentPosition
        {
            get 
            {
                lock (_syncLock)
                {
                    return _mp3Reader == null ? TimeSpan.Zero : _mp3Reader.CurrentTime;
                }
            }
            set
            {
                lock (_syncLock)
                {
                    if (_mp3Reader != null)
                    {
                        _mp3Reader.CurrentTime = value;
                    }
                }
            }
        }

        public TimeSpan TotalLength
        {
            get
            {
                lock (_syncLock)
                {
                    return _mp3Reader == null ? TimeSpan.Zero : _mp3Reader.TotalTime;
                }
            }
        }

        public PlaybackResult PlayFile(string filePath)
        {
            return Play(filePath, true, 0, 0);
        }

        public PlaybackResult PlayFile(string filePath, float volumeDb, int skipMs)
        {
            return Play(filePath, true, volumeDb, skipMs);
        }

        // Example of raw data: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
        public PlaybackResult PlayRawData(string rawData)
        {
            return Play(rawData, false, 0, 0);
        }

        public PlaybackResult PlayRawData(string rawData, float volumeDb, int skipMs)
        {
            return Play(rawData, false, volumeDb, skipMs);
        }

        private PlaybackResult Play(string mp3Data, bool isFilePath, float volumeDb, int skipMs)
        {
            if (!Monitor.TryEnter(_playLock))
                throw new InvalidOperationException("Only one audio file can be played at a time!");

            try
            {
                using (var inputStream = isFilePath 
                    ? new Mp3FileReader(mp3Data)
                    : new Mp3FileReader(new MemoryStream(Convert.FromBase64String(mp3Data))))
                {
                    if (skipMs > 0)
                    {
                        inputStream.CurrentTime = TimeSpan.FromMilliseconds(skipMs);
                    }
                    return PlayStream(inputStream, volumeDb);
                }
            }
            finally
            {
                Monitor.Exit(_playLock);
            }
        }

        public void Resume()
        {
            lock (_syncLock)
            {
                if (_wavePlayer != null && _wavePlayer.PlaybackState == PlaybackState.Paused)
                {
                    _wavePlayer.Play();
                }
            }
        }

        public void Pause()
        {
            lock (_syncLock)
            {
                if (_wavePlayer != null && _wavePlayer.PlaybackState == PlaybackState.Playing)
                {
                    _wavePlayer.Pause(); 
                }
            }
        }

        public void Stop()
        {
            lock (_syncLock)
            {
                if (_wavePlayer != null && _wavePlayer.PlaybackState != PlaybackState.Stopped)
                {
                    _wavePlayer.Stop();
                }
            }
        }

        private PlaybackResult PlayStream(Mp3FileReader inputStream, float volumeDb)
        {
            using (var waveOutDevice = new WaveOutEvent())
            {
                waveOutDevice.PlaybackStopped += new EventHandler<StoppedEventArgs>(_waveOutDevice_PlaybackStopped);

                // WaveChannel32 doesn't raise 'PlaybackStopped' event
                var channel = new SampleChannel(inputStream);
                channel.Volume = ConvetVolumeDb(volumeDb);

                SamplesCollector collector = null;
                if (_collectSamples)
                {
                    collector = new SamplesCollector(channel);
                }

                waveOutDevice.Init(new SampleToWaveProvider((ISampleProvider)collector ?? channel));
                waveOutDevice.Play();

                lock (_syncLock)
                {
                    _mp3Reader = inputStream;
                    _wavePlayer = waveOutDevice;
                }

                _stopWait.WaitOne();

                lock (_syncLock)
                {
                    _mp3Reader = null;
                    _wavePlayer = null;
                }

                if (_lastError != null)
                    throw _lastError;

                PlaybackResult result = new PlaybackResult();
                if (_collectSamples)
                {
                    result.CollectSamples(collector);
                }

                return result;
            }
        }

        private void _waveOutDevice_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            _lastError = e.Exception;
            _stopWait.Set();
        }

        private float ConvetVolumeDb(float volumeDb)
        {
            // We can only reduce volume, not amplify
            if (volumeDb >= 0)
                return 1;

            var volume = (float)Math.Pow(10, volumeDb / 20);
            return (volume > 1 || volume <= 0) ? 1 : volume;

            //float db = 20 * (float)Math.Log10(Volume);
        }
    }
}
