using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;

namespace Pronunciation.Trainer.Utility
{
    public class WaveFormBuilder
    {
        public struct AudioSamplePoint
        {
            public int X;
            public double YMax;
            public double YMin;
        }

        private delegate void AlgorithmDelegate(float[] data, int startIndex, int endIndex, out float max, out float min);
        private readonly static AlgorithmDelegate Algorithm = AlgorithmMax;

        public List<AudioSamplePoint> DrawNormalizedAudio(float[] data, double maxWidth, double maxHeight)
        {
            double dataToSize = (double)data.Length / maxWidth;

            List<AudioSamplePoint> samplePoints = new List<AudioSamplePoint>();
            double maxSample = 0;
            for (int iPixel = 0; iPixel < maxWidth; iPixel++)
            {
                // determine start and end points within WAV
                int start = (int)((double)iPixel * dataToSize);
                int end = (int)((double)(iPixel + 1) * dataToSize);
                if (end > data.Length)
                {
                    end = data.Length;
                }

                float max;
                float min;
                Algorithm(data, start, end, out max, out min);
                samplePoints.Add(new AudioSamplePoint { X = iPixel, YMax = max, YMin = min});

                if (Math.Abs(max) > maxSample)
                {
                    maxSample = Math.Abs(max);
                }
                if (Math.Abs(min) > maxSample)
                {
                    maxSample = Math.Abs(min);
                }
            }

            // Normalize all samples so that they fit the entire height
            double amplifier = 1;
            if (maxSample < 1 && maxSample > 0)
            {
                amplifier = 1 / maxSample;
            }

            for (int i = 0; i < samplePoints.Count; i++)
            {
                var p = samplePoints[i];
                p.YMax = 0.5 * maxHeight * (1 - p.YMax * amplifier);
                p.YMin = 0.5 * maxHeight * (1 - p.YMin * amplifier);

                if (p.YMax > maxHeight)
                {
                    p.YMax = maxHeight;
                }
                if (p.YMin > maxHeight)
                {
                    p.YMin = maxHeight;
                }

                samplePoints[i] = p; // because this is a structure
            }

            return samplePoints;
        }

        private static void AlgorithmMax(float[] data, int startIndex, int endIndex, out float max, out float min)
        {
            min = float.MaxValue;
            max = float.MinValue;
            if (startIndex == endIndex)
            {
                min = max = 0;
            }
            else
            {
                for (int i = startIndex; i < endIndex; i++)
                {
                    float val = data[i];
                    min = val < min ? val : min;
                    max = val > max ? val : max;
                }
                if (max == min)
                {
                    min = 0;
                }
            }
        }

        private static void AlgorithmAverage(float[] data, int startIndex, int endIndex, out float posAvg, out float negAvg)
        {
            posAvg = 0;
            negAvg = 0;

            int posCount = 0;
            int negCount = 0;
            for (int i = startIndex; i < endIndex; i++)
            {
                if (data[i] > 0)
                {
                    posCount++;
                    posAvg += data[i];
                }
                else
                {
                    negCount++;
                    negAvg += data[i];
                }
            }

            if (posCount > 0)
            {
                posAvg /= posCount;
            }
            if (negCount > 0)
            {
                negAvg /= negCount;
            }
        }
    }
}
