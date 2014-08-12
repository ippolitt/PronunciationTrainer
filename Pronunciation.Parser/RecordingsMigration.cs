using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlServerCe;
using System.IO;
using System.Data;

namespace Pronunciation.Parser
{
    public class RecordingsMigration
    {
        private enum AudioTargetType
        {
            LPD = 1,
            Exercise = 2,
            Training = 3,
            QuickRecorder = 4
        }

        private readonly SqlCeConnection _conn;
        private const string SourceFolder = @"D:\LEARN\English\Pronunciation\Trainer";

        public RecordingsMigration(SqlCeConnection conn)
        {
            _conn = conn;
        }

        public void Migrate()
        {
            MigrateTrainings();
            MigrateLPD();
            MigrateExercises();
        }

        private void Log(string text, params string[] args)
        {
            File.AppendAllText(@"D:\Temp\RecordingsMigration.txt", string.Format(text, args) + Environment.NewLine);
        }

        private void MigrateLPD()
        {
            Console.WriteLine("Started LPD migration");
            CleanRecordings(AudioTargetType.LPD);

            string recordingsRoot = Path.Combine(SourceFolder, @"RecordedAudio\LPD");

            SqlCeCommand cmdRecordings = new SqlCeCommand("RecordedAudio", _conn);
            cmdRecordings.CommandType = CommandType.TableDirect;
            using (SqlCeResultSet rsRecordings = cmdRecordings.ExecuteResultSet(ResultSetOptions.Updatable))
            {
                int recordingsCount = 0;
                foreach (string recordingsFile in Directory.GetFiles(recordingsRoot, "*.mp3", SearchOption.TopDirectoryOnly))
                {
                    string soundKey = Path.GetFileNameWithoutExtension(recordingsFile);
                    var recording = rsRecordings.CreateRecord();
                    FillRecording(recording, AudioTargetType.LPD, string.Format("lpd|{0}", soundKey), recordingsFile);
                    rsRecordings.Insert(recording);

                    recordingsCount++;
                    if (recordingsCount % 100 == 0)
                    {
                        Console.WriteLine("Processed {0} LPD recordings", recordingsCount);
                    }
                }
            }

            Console.WriteLine("Finished LPD migration");
        }

        private void MigrateTrainings()
        {
            Console.WriteLine("Started training migration");
            CleanRecordings(AudioTargetType.Training);

            string recordingsRoot = Path.Combine(SourceFolder, @"RecordedAudio\Recordings");

            SqlCeCommand cmdRecordings = new SqlCeCommand("RecordedAudio", _conn);
            cmdRecordings.CommandType = CommandType.TableDirect;
            using (SqlCeResultSet rsRecordings = cmdRecordings.ExecuteResultSet(ResultSetOptions.Updatable))
            {
                foreach (string trainingFolder in Directory.GetDirectories(recordingsRoot, "*", SearchOption.TopDirectoryOnly))
                {
                    bool hasRecordings = false;
                    Guid trainingId = new Guid(new DirectoryInfo(trainingFolder).Name);
                    foreach (string recordingsFile in Directory.GetFiles(trainingFolder, "*.mp3", SearchOption.TopDirectoryOnly))
                    {
                        var recording = rsRecordings.CreateRecord();
                        FillRecording(recording, AudioTargetType.Training, string.Format("{0}", trainingId), recordingsFile);
                        rsRecordings.Insert(recording);

                        hasRecordings = true;
                    }

                    if (!hasRecordings)
                    {
                        Log("WARNING: training '{0}' has no recordings!"); 
                    }
                }
            }

            Console.WriteLine("Finished training migration");
        }

        private void MigrateExercises()
        {
            Console.WriteLine("Started exercise migration");
            CleanExerciseData();
            CleanRecordings(AudioTargetType.Exercise);

            string exerciseRoot = Path.Combine(SourceFolder, "Exercises");
            string recordingsRoot = Path.Combine(SourceFolder, @"RecordedAudio\Exercises");
            Dictionary<int, string> books = LoadBooks();

            SqlCeCommand cmdExercise = new SqlCeCommand("Exercise", _conn);
            cmdExercise.CommandType = CommandType.TableDirect;

            SqlCeCommand cmdAudio = new SqlCeCommand("ExerciseAudio", _conn);
            cmdAudio.CommandType = CommandType.TableDirect;

            SqlCeCommand cmdRecordings = new SqlCeCommand("RecordedAudio", _conn);
            cmdRecordings.CommandType = CommandType.TableDirect;

            SqlCeResultSet rsExercise = cmdExercise.ExecuteResultSet(ResultSetOptions.Updatable);
            SqlCeResultSet rsAudio = cmdAudio.ExecuteResultSet(ResultSetOptions.Updatable);
            SqlCeResultSet rsRecordings = cmdRecordings.ExecuteResultSet(ResultSetOptions.Updatable);
            try
            {
                int exerciseCount = 0;
                while (rsExercise.Read())
                {
                    int bookId = rsExercise.GetInt32(rsExercise.GetOrdinal("BookId"));
                    int cd = rsExercise.GetInt32(rsExercise.GetOrdinal("SourceCD"));
                    int track = rsExercise.GetInt32(rsExercise.GetOrdinal("SourceTrack"));

                    string relativeExercisePath = Path.Combine(books[bookId], string.Format("CD{0}", cd), track.ToString());
                    string exerciseFolder = Path.Combine(exerciseRoot, relativeExercisePath);
                    string recordingsFolder = Path.Combine(recordingsRoot, relativeExercisePath);
                    
                    string imagePath = Path.Combine(exerciseFolder, "main.png");
                    if (!File.Exists(imagePath))
                    {
                        Log("WARNING: image '{0}' doesn't exist!", relativeExercisePath);
                    }
                    else
                    {
                        rsExercise.SetValue(rsExercise.GetOrdinal("ExerciseData"), File.ReadAllBytes(imagePath));
                        rsExercise.Update();
                    }

                    Guid exerciseId = rsExercise.GetGuid(rsExercise.GetOrdinal("ExerciseId"));
                    foreach (string audioFile in Directory.GetFiles(exerciseFolder, "*.mp3", SearchOption.TopDirectoryOnly))
                    {
                        string audioName = Path.GetFileNameWithoutExtension(audioFile);

                        var audio = rsAudio.CreateRecord();
                        audio["AudioId"] = Guid.NewGuid();
                        audio["ExerciseId"] = exerciseId;
                        audio["AudioName"] = audioName;
                        audio["RawData"] = File.ReadAllBytes(audioFile);
                        rsAudio.Insert(audio);

                        string recordingsFile = Path.Combine(recordingsFolder, string.Format("{0}.mp3", audioName));
                        if (File.Exists(recordingsFile))
                        {
                            var recording = rsRecordings.CreateRecord();
                            FillRecording(recording, AudioTargetType.Exercise, string.Format("{0}|{1}", exerciseId, audioName), recordingsFile);
                            rsRecordings.Insert(recording);
                        }
                        else
                        {
                            Log("WARNING: folder '{0}' is missing a recording '{1}'", relativeExercisePath, audioName);
                        }
                    }

                    exerciseCount++;
                    if (exerciseCount % 20 == 0)
                    {
                        Console.WriteLine("Processed {0} exercises", exerciseCount);
                    }
                }
            }
            finally
            {
                rsExercise.Close();
                rsAudio.Close();
                rsRecordings.Close();
            }

            Console.WriteLine("Finished exercise migration");
        }

        private void FillRecording(SqlCeUpdatableRecord record, AudioTargetType targetType, string targetKey, string filePath)
        {
            record["AudioId"] = Guid.NewGuid();
            record["TargetKey"] = targetKey;
            record["TargetTypeId"] = (int)targetType;
            record["Recorded"] = File.GetCreationTime(filePath);
            record["RawData"] = File.ReadAllBytes(filePath);
        }

        private void CleanExerciseData()
        {
            SqlCeCommand cmd1 = new SqlCeCommand(
@"UPDATE Exercise SET ExerciseData = null", _conn);
            int count1 = cmd1.ExecuteNonQuery();
            Console.WriteLine("Update {0} exercises", count1);

            SqlCeCommand cmd2 = new SqlCeCommand(
@"DELETE FROM ExerciseAudio", _conn);
            int count2 = cmd2.ExecuteNonQuery();
            Console.WriteLine("Deleted {0} exercise audios", count2);
        }

        private void CleanRecordings(AudioTargetType audioTargetType)
        {
            SqlCeCommand cmd = new SqlCeCommand(
@"DELETE FROM RecordedAudio
 WHERE TargetTypeId = @targetTypeId", _conn);
            cmd.Parameters.AddWithValue("@targetTypeId", (int)audioTargetType);

            int count = cmd.ExecuteNonQuery();
        }

        private Dictionary<int, string> LoadBooks()
        {
            var results = new Dictionary<int, string>();

            SqlCeCommand cmd = new SqlCeCommand(@"SELECT BookId, ShortName FROM Book", _conn);
            using (var reader = cmd.ExecuteReader())
            {
                while(reader.Read())
                {
                    results.Add((int)reader["BookId"], (string)reader["ShortName"]);
                }
            }

            return results;
        }
    }
}
