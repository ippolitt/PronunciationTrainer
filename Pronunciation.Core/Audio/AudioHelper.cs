﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Pronunciation.Core.Audio
{
    public class AudioHelper
    {
        public static int GetAudioLengthMs(string filePath)
        {
            return ConvertDuration(GetAudioInfo(filePath).Duration);
        }

        public static int GetAudioLengthMs(byte[] rawData)
        {
            return ConvertDuration(GetAudioInfo(rawData).Duration);
        }

        public static AudioInfo GetAudioInfo(string filePath)
        {
            using (FileStream stream = File.OpenRead(filePath))
            {
                return GetAudioInfo(stream);
            }
        }

        public static AudioInfo GetAudioInfo(byte[] rawData)
        {
            using (MemoryStream stream = new MemoryStream(rawData))
            {
                return GetAudioInfo(stream);
            }
        }

        public static AudioSamples CollectSamples(string filePath)
        {
            return CollectSamples(filePath, null);
        }

        public static AudioSamples CollectSamples(byte[] rawData)
        {
            return CollectSamples(rawData, null);
        }

        public static AudioSamples CollectSamples(string filePath, AudioSamplesProcessingArgs processingArgs)
        {
            using (FileStream stream = File.OpenRead(filePath))
            {
                return CollectSamples(stream, processingArgs);
            }
        }

        public static AudioSamples CollectSamples(byte[] rawData, AudioSamplesProcessingArgs processingArgs)
        {
            using (MemoryStream stream = new MemoryStream(rawData))
            {
                return CollectSamples(stream, processingArgs);
            }
        }

        private static AudioInfo GetAudioInfo(Stream input)
        {
            int samplesCount = 0;
            int sampleRate = 0;
            double durationMs = 0;
            Mp3Frame frame;
            while (true)
            {
                frame = Mp3Frame.LoadFromStream(input);
                if (frame == null)
                    break;

                // Sometimes it returns different SampleRate for the first frames so we must calculate the duration for each frame separately
                durationMs += 1000 * (double)frame.SampleCount / (double)frame.SampleRate;
                samplesCount += frame.SampleCount;
                sampleRate = frame.SampleRate;
            }

            // Return sampleRate from the last frame 
            return new AudioInfo(ConvertDuration(durationMs), samplesCount, sampleRate);
        }

        private static AudioSamples CollectSamples(Stream input, AudioSamplesProcessingArgs processingArgs)
        {
            var leftSamples = new List<float>();
            var rightSamples = new List<float>();
            TimeSpan samplesDuration;
            TimeSpan? totalDuration = null; 
            using (var reader = new Mp3FileReader(input))
            {
                if (processingArgs != null && processingArgs.StartFrom != TimeSpan.Zero)
                {
                    reader.CurrentTime = processingArgs.StartFrom;
                }

                ISampleProvider sampleProvider = new SampleChannel(reader);
                int channelsCount = reader.WaveFormat.Channels;
                int sampleRate = reader.WaveFormat.SampleRate;

                float[] readBuffer = new float[1024];
                bool isAborted = false;
                int samplesCount = 0;
                bool collectRightChannel = (processingArgs == null || !processingArgs.CollectOneChannelOnly);
                int collectionStep = (processingArgs == null || processingArgs.CollectionStep <= 0) ? 1 : processingArgs.CollectionStep;
                while (true)
                {
                    int samplesRead = sampleProvider.Read(readBuffer, 0, readBuffer.Length);
                    if (samplesRead <= 0)
                        break;

                    if (processingArgs != null && processingArgs.AbortToken != null && processingArgs.AbortToken.IsCancellationRequested)
                    {
                        if (processingArgs.AbortToken.IsThrowCanceledException)
                            throw new OperationCanceledException();

                        isAborted = true;
                        break;
                    }

                    for (int i = 0; i < samplesRead; i += channelsCount)
                    {
                        if (samplesCount % collectionStep == 0)
                        {
                            leftSamples.Add(readBuffer[i]);
                            if (channelsCount > 1 && collectRightChannel)
                            {
                                rightSamples.Add(readBuffer[i + 1]);
                            }
                        }
                        samplesCount++;
                    }
                }

                samplesDuration = ConvertDuration(1000 * (double)samplesCount / (double)sampleRate);
                if (!isAborted)
                {
                    totalDuration = samplesDuration;
                }
            }

            return rightSamples.Count == 0
                ? new AudioSamples(samplesDuration, totalDuration, leftSamples.ToArray())
                : new AudioSamples(samplesDuration, totalDuration, leftSamples.ToArray(), rightSamples.ToArray());
        }

        private static int ConvertDuration(TimeSpan duration)
        {
            return (int)Math.Round(duration.TotalMilliseconds);
        }

        private static TimeSpan ConvertDuration(double durationMs)
        {
            return TimeSpan.FromMilliseconds(Math.Round(durationMs));
        }
    }
}
