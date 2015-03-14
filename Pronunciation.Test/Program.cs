using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using Pronunciation.Core.Audio;
using System.Diagnostics;
using System.Data.SqlServerCe;
using System.Data;
using Pronunciation.Core.Utility;

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
//                bool gg = string.Equals(null, null);
                //string str = "sec".Substring(1, 0);
                //TestBigFile();
                //TestLoadingData();
                
                string s = @"alˌ52";
                var blde = new StringBuilder(s.Substring(0, 0));
                bool bo = s.StartsWith("al", StringComparison.Ordinal);

                if (args != null && args.Length == 1)
                {
                    string filePath = args[0];
                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    if (!File.Exists(filePath))
                        return;

                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        stream.ReadByte();
                    }
                    watch.Stop();
                    Console.WriteLine("Elapsed {0} ms", watch.ElapsedMilliseconds);
                }

                //var files = Directory.GetFiles(@"D:\WORK\NET\PronunciationTrainer\Data\MW\Active");
                //foreach (var file in files)
                //{
                //    File.Move(file, file.Replace(".mp3", ".wav"));                   
                //}

                var builder = new StringBuilder("hello");
                builder.Replace("l", "p");

                List<int> l1 = new List<int> { 1, 2, 3 };
                List<int> l2 = new List<int> { 8, 4, 2, 9, 4 };

                l1.AddRange(l2.Where(x => !l1.Contains(x)));

                bool? rr = null;

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

        private  static void RenameAudios()
        {
            using (SqlCeConnection conn = new SqlCeConnection(TrainerConnectionString))
            {
                conn.Open();

                SqlCeCommand cmdRead = new SqlCeCommand("SELECT ExerciseId FROM Exercise WHERE Title = 'Exercise 4-6: Rule 4 - \"Held T\" Before N'", conn);
                var exerciseId = (Guid)cmdRead.ExecuteScalar();

                SqlCeCommand cmdRecorded = new SqlCeCommand(string.Format("SELECT AudioId, TargetKey FROM RecordedAudio WHERE TargetKey like '{0}|%'", exerciseId), conn);
                var resultset = cmdRecorded.ExecuteResultSet(ResultSetOptions.Updatable | ResultSetOptions.Scrollable);
                foreach(SqlCeUpdatableRecord row in resultset)
                {
                    var key = row.GetString(1).Split('|')[1];
                   // resultset.SetString(1, string.Format("{0}|B{1}", exerciseId, key));
                    //resultset.Update();
                }
                //
                //SqlCeCommand cmdUpd = new SqlCeCommand("UPDATE ExerciseAudio SET AudioName = 'B' + AudioName WHERE ExerciseId=@id", conn);
                //cmdUpd.Parameters.AddWithValue("@id", exerciseId);
                //int records = cmdUpd.ExecuteNonQuery();
            }
        }

        private static void TestLoadingData()
        {
            DATFileReader reader = new DATFileReader(@"D:\LEARN\English\Pronunciation\Trainer\Database\audio_auto.dat");
            var data = reader.GetData(174466, 4854);

            Mp3Player player = new Mp3Player();
            player.PlayRawData(data);
        }

        private static void TestBigFile()
        {
            byte[] data = new byte[] { 34, 126, 45 };
            using (var stream = new FileStream(@"D:\Temp\Sample.txt", FileMode.Append, FileAccess.Write, FileShare.Read))
            {
                if (stream.Position == 0)
                {
                    stream.WriteByte(1);
                }

                long offset = stream.Position;
                stream.Write(data, 0, data.Length);
                long length = stream.Position - offset;
                stream.WriteByte(1);
            }

            //4000, FileOptions.SequentialScan
            var filePath = @"D:\WORK\NET\PronunciationTrainer\Data\LPD\Sounds.zip";
            Stopwatch watch = new Stopwatch();
            watch.Start();
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var buffer = new byte[10000];
                var signature = new byte[2];
                stream.Seek(500000000, SeekOrigin.Begin);
                stream.Read(signature, 0, signature.Length);
                int ss = stream.Read(buffer, 0, buffer.Length);
                stream.Read(signature, 0, signature.Length);
                long after = stream.Position;
            }
            watch.Stop();
            Console.WriteLine(watch.ElapsedTicks);
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
