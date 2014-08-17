using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using Pronunciation.Core.Audio;
using System.Diagnostics;

namespace Pronunciation.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // "D:\LEARN\English\Pronunciation\Backup\Exercises\AC\CD4\48"
                // "D:\LEARN\English\Pronunciation\Backup\Exercises\AC2012\CD1\55"
                // "D:\Temp\Recordings"
                TestAudio(@"D:\Temp\Recordings", true);

                return;

                bool? rr = false;

                if (rr == false)
                {
                    Console.WriteLine("OK");
                }
                Console.WriteLine("{0:00}-{1:00}", 3, 4);
                int number;
                bool res = int.TryParse(null, out number);

                int k = "".CompareTo(null);

                //Image i = Image.FromFile(@"D:\WORK\NET\PronunciationTrainer\Pronunciation.Trainer\Resources\AudioWaveform.png");
                //Console.WriteLine(i.HorizontalResolution + " " + i.VerticalResolution);
                //Console.WriteLine(i.Width + " " + i.Height);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                Console.WriteLine("Finished");
                Console.ReadLine();
            }
        }

        private static void TestAudio(string folder, bool isSamples)
        {
            string[] audioFiles = Directory.GetFiles(folder, "*.mp3", SearchOption.TopDirectoryOnly);
            var watch = new Stopwatch();
            double totalDuration = 0;

            watch.Start();
            foreach (var audioFile in audioFiles)
            {
                if (isSamples)
                {
                    var samples = AudioHelper.CollectSamples(audioFile);
                    Console.WriteLine("File: {0}, sample count: {1}, is stereo: {2}", Path.GetFileNameWithoutExtension(audioFile), 
                        samples.LeftChannel.Length, samples.IsStereo); 
                }
                else
                {
                    var audioDuration = AudioHelper.GetAudioLength(audioFile);
                    totalDuration += audioDuration.TotalMilliseconds;
                    //Console.WriteLine("File: {0}, duration: {1} ms", Path.GetFileNameWithoutExtension(audioFile), audioDuration.TotalMilliseconds);   
                }
            }
            watch.Stop();

            Console.WriteLine("Total duration: {0} s", Math.Round(totalDuration/1000));
            Console.WriteLine("Time elapsed: {0} ms", watch.ElapsedMilliseconds);
        }
    }
}
