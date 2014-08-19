﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using Pronunciation.Core.Audio;
using System.Diagnostics;
using System.Data.SqlServerCe;
using System.Data;

namespace Pronunciation.Test
{
    class Program
    {
        private const string TrainerConnectionString =
@"Data Source=D:\LEARN\English\Pronunciation\Trainer\Database\PronunciationTrainer.sdf;Max Database Size=2000;";

        static void Main(string[] args)
        {
            try
            {
                // "D:\LEARN\English\Pronunciation\Backup\Exercises\AC\CD4\48"
                // "D:\LEARN\English\Pronunciation\Backup\Exercises\AC2012\CD1\55"
                // "D:\Temp\Recordings"
                //TestAudio(@"D:\Temp\Recordings", true);
                //TimeSpan ts = TimeSpan.FromMilliseconds( 0);
                //Console.WriteLine(ToTimeString(ts));
                //MigrateExerciseDurations();
                //MigrateRecordingDurations();
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

        public static string ToTimeString(TimeSpan ts)
        {
            int hours = Math.Abs(ts.Days * 24 + ts.Hours);
            int minutes = Math.Abs(ts.Minutes);
            int seconds = (int)Math.Ceiling(Math.Abs(ts.Seconds + (double)ts.Milliseconds / 1000));

            return string.Format("{0}{1}{2}:{3:00}", 
                ts < TimeSpan.Zero ? "-" : null,
                hours > 0 ? string.Format("{0}:", hours) : null,
                string.Format(hours > 0 ? "{0:00}" : "{0:#0}", minutes),
                seconds);
        }

        private static T GetResult<T>()
        {
            return default(T);
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
                    var audioDuration = AudioHelper.GetAudioInfo(audioFile).Duration;
                    totalDuration += audioDuration.TotalMilliseconds;
                    //Console.WriteLine("File: {0}, duration: {1} ms", Path.GetFileNameWithoutExtension(audioFile), audioDuration.TotalMilliseconds);   
                }
            }
            watch.Stop();

            Console.WriteLine("Total duration: {0} s", Math.Round(totalDuration/1000));
            Console.WriteLine("Time elapsed: {0} ms", watch.ElapsedMilliseconds);
        }

        private static void MigrateExerciseDurations()
        {
            using (SqlCeConnection conn = new SqlCeConnection(TrainerConnectionString))
            {
                conn.Open();
                // return;

                SqlCeCommand cmdRead = new SqlCeCommand("SELECT AudioId, RawData FROM ExerciseAudio", conn);
               
                SqlCeCommand cmdUpd = new SqlCeCommand("UPDATE ExerciseAudio SET Duration = @duration WHERE AudioId=@id", conn);
                var parmId = cmdUpd.Parameters.Add("@id", SqlDbType.UniqueIdentifier);
                var parmValue = cmdUpd.Parameters.Add("@duration", SqlDbType.Int);
                using (var reader = cmdRead.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        parmId.Value = (Guid)reader["AudioId"];
                        parmValue.Value = AudioHelper.GetAudioLengthMs((byte[])reader["RawData"]);
                        int k = cmdUpd.ExecuteNonQuery();
                        if (k != 1)
                            throw new InvalidOperationException();
                    }
                }
            }
        }

        private static void MigrateRecordingDurations()
        {
            using (SqlCeConnection conn = new SqlCeConnection(TrainerConnectionString))
            {
                conn.Open();
                // return;

                SqlCeCommand cmdRead = new SqlCeCommand("SELECT AudioId, RawData FROM RecordedAudio", conn);

                SqlCeCommand cmdUpd = new SqlCeCommand("UPDATE RecordedAudio SET Duration = @duration WHERE AudioId=@id", conn);
                var parmId = cmdUpd.Parameters.Add("@id", SqlDbType.UniqueIdentifier);
                var parmValue = cmdUpd.Parameters.Add("@duration", SqlDbType.Int);
                using (var reader = cmdRead.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        parmId.Value = (Guid)reader["AudioId"];
                        parmValue.Value = AudioHelper.GetAudioLengthMs((byte[])reader["RawData"]);
                        int k = cmdUpd.ExecuteNonQuery();
                        if (k != 1)
                            throw new InvalidOperationException();
                    }
                }
            }
        }
    }
}
