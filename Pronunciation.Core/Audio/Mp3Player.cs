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
    public class Mp3Player : IDisposable
    {
        private AutoResetEvent _stopWait;
        private Exception _lastError;
        private IWavePlayer _wavePlayer;
        private Mp3FileReader _mp3Reader;

        private bool _isDisposed;
        private TimeSpan _totalLength = TimeSpan.Zero;
        private readonly bool _collectSamples;
        private readonly object _syncLock = new object();
        private static readonly object _playLock = new object(); // Allow only one active playback per process

        public delegate void PlayerStateChangedDelegate(Mp3Player player);

        public event PlayerStateChangedDelegate PlayingStarted;
        public event PlayerStateChangedDelegate PlayingCompleted; 

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
            get { return _totalLength; }
        }

        public PlaybackResult PlayFile(string filePath)
        {
            return PlayFile(filePath, 0, 0);
        }

        public PlaybackResult PlayFile(string filePath, float volumeDb, int skipMs)
        {
            return Play(() => new Mp3FileReader(filePath), volumeDb, skipMs);
        }

        public PlaybackResult PlayRawData(byte[] data)
        {
            return PlayRawData(data, 0, 0);
        }

        public PlaybackResult PlayRawData(byte[] data, float volumeDb, int skipMs)
        {
            return Play(() => new Mp3FileReader(new MemoryStream(data)), volumeDb, skipMs);
        }

        private PlaybackResult Play(Func<Mp3FileReader> readerBuilder, float volumeDb, int skipMs)
        {
            if (_isDisposed)
                throw new InvalidOperationException("The player has been disposed!");

            if (!Monitor.TryEnter(_playLock))
                throw new InvalidOperationException("Only one audio file can be played at a time!");

            try
            {
                using (var inputStream = readerBuilder())
                {
                    _totalLength = inputStream.TotalTime;
                    return PlayStream(inputStream, volumeDb, 
                        skipMs > 0 ? TimeSpan.FromMilliseconds(skipMs) : TimeSpan.Zero);
                }
            }
            finally
            {
                Monitor.Exit(_playLock);
            }
        }

        public bool Resume()
        {
            lock (_syncLock)
            {
                if (_wavePlayer != null && _wavePlayer.PlaybackState == PlaybackState.Paused)
                {
                    _wavePlayer.Play();
                    return true;
                }
            }

            return false;
        }

        public bool Pause()
        {
            lock (_syncLock)
            {
                if (_wavePlayer != null && _wavePlayer.PlaybackState == PlaybackState.Playing)
                {
                    _wavePlayer.Pause();
                    return true;
                }
            }

            return false;
        }

        public bool Stop()
        {
            lock (_syncLock)
            {
                if (_wavePlayer != null && _wavePlayer.PlaybackState != PlaybackState.Stopped)
                {
                    _wavePlayer.Stop();
                    return true;
                }
            }

            return false;
        }

        private PlaybackResult PlayStream(Mp3FileReader inputStream, float volumeDb, TimeSpan skippedTime)
        {
            if (skippedTime != TimeSpan.Zero)
            {
                inputStream.CurrentTime = skippedTime;
            }

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
                if (PlayingStarted != null)
                {
                    PlayingStarted(this);
                }

                // Here the thread is blocked until PlaybackStopped method signals the wait handle
                _stopWait.WaitOne();

                if (PlayingCompleted != null)
                {
                    PlayingCompleted(this);
                }
                lock (_syncLock)
                {
                    _mp3Reader = null;
                    _wavePlayer = null;
                }

                if (_lastError != null)
                    throw _lastError;

                return collector == null
                    ? new PlaybackResult(inputStream.TotalTime)
                    : new PlaybackResult(inputStream.TotalTime,
                        CalculateSamplesDuration(collector.Samples.Count, inputStream, skippedTime), 
                        collector.Samples);
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

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _stopWait.Dispose();
                _isDisposed = true;
            }
        }

        private TimeSpan CalculateSamplesDuration(int samplesCount, Mp3FileReader reader, TimeSpan skippedTime)
        {
            if (samplesCount <= 0)
                return reader.TotalTime;

            int sampleRate = reader.WaveFormat.SampleRate; // Samples per second
            int channelsCount = reader.WaveFormat.Channels; // Number of channels: 1 - for mono, 2 - for stereo

            var result = TimeSpan.FromSeconds((double)samplesCount / (sampleRate * channelsCount)).Add(skippedTime);

            // If Played time is very close to the Total time then consider they are equal (calculation innacuracy)
            if (Math.Abs(reader.TotalTime.Subtract(result).TotalMilliseconds) <= 100)
            {
                result = reader.TotalTime;
            }

            return result;
        }
    }
}
