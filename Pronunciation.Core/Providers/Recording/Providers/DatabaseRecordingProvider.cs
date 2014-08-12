using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlServerCe;
using System.Data;
using System.IO;
using Pronunciation.Core.Database;
using Pronunciation.Core.Contexts;
using Pronunciation.Core.Providers.Recording.HistoryPolicies;

namespace Pronunciation.Core.Providers.Recording.Providers
{
    public class DatabaseRecordingProvider<T> : IRecordingProvider<T> where T : IDatabaseTargetKey
    {
        private readonly string _connectionString;
        private readonly string _workingFolder;
        private const string WorkingFolderName = "Recordings";

        public DatabaseRecordingProvider(string connectionString, string tempFolder)
        {
            _connectionString = connectionString;

            _workingFolder = Path.Combine(tempFolder, WorkingFolderName);
            if (!Directory.Exists(_workingFolder))
            {
                Directory.CreateDirectory(_workingFolder);
            }
        }

        public bool ContainsAudios(T targetKey)
        {
            using (SqlCeConnection conn = new SqlCeConnection(_connectionString))
            {
                conn.Open();

                var cmd = new SqlCeCommand(string.Format(
@"SELECT TOP(1) 1
FROM RecordedAudio
WHERE TargetKey = @key AND TargetTypeId = {0}", targetKey.TargetTypeId), conn);
                cmd.Parameters.AddWithValue("@key", targetKey.TargetKey);

                var result = cmd.ExecuteScalar();
                return result != null;
            }
        }

        public RecordedAudioListItem[] GetAudioList(T targetKey)
        {
            var results = new List<RecordedAudioListItem>();
            using (SqlCeConnection conn = new SqlCeConnection(_connectionString))
            {
                conn.Open();

                var cmd = new SqlCeCommand(string.Format(
@"SELECT AudioId, TargetKey, Recorded
FROM RecordedAudio
WHERE TargetKey = @key AND TargetTypeId = {0}", targetKey.TargetTypeId), conn);
                cmd.Parameters.AddWithValue("@key", targetKey.TargetKey);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new RecordedAudioListItem
                        {
                            AudioKey = ((Guid)reader["AudioId"]).ToString(),
                            RecordingDate = (DateTime)reader["Recorded"]
                        });
                    }
                }
            }

            return results.OrderByDescending(x => x.RecordingDate).ToArray();
        }

        public PlaybackData GetLatestAudio(T targetKey)
        {
            RecordedAudio latestAudio = GetLatestAudio(targetKey, true);
            return (latestAudio == null || latestAudio.RawData == null) ? null : new PlaybackData(latestAudio.RawData);
        }

        private RecordedAudio GetLatestAudio(T targetKey, bool loadData)
        {
            using (SqlCeConnection conn = new SqlCeConnection(_connectionString))
            {
                conn.Open();

                var cmd = new SqlCeCommand(string.Format(
@"SELECT TOP(1) AudioId, Recorded, TargetKey, TargetTypeId, {0}RawData 
FROM RecordedAudio
WHERE TargetKey = @key AND TargetTypeId = {1}
ORDER BY Recorded DESC", 
                       loadData ? null : "NULL AS ",
                       targetKey.TargetTypeId), 
                    conn);
                cmd.Parameters.AddWithValue("@key", targetKey.TargetKey);

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    return new RecordedAudio
                    {
                        AudioId = (Guid)reader["AudioId"],
                        Recorded = (DateTime)reader["Recorded"],
                        TargetKey = (string)reader["TargetKey"],
                        TargetTypeId = (int)reader["TargetTypeId"],
                        RawData = reader["RawData"] as byte[]
                    };
                }
            }
        }

        public PlaybackData GetAudio(T targetKey, string audioKey)
        {
            using (SqlCeConnection conn = new SqlCeConnection(_connectionString))
            {
                conn.Open();

                var cmd = new SqlCeCommand(
@"SELECT RawData 
FROM RecordedAudio
WHERE AudioId = @id", conn);
                cmd.Parameters.AddWithValue("@id", new Guid(audioKey));

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        throw new ArgumentException(string.Format(
                            "Audio with id='{0}' doesn't exist!", audioKey));
                    }

                    var data = reader["RawData"] as byte[];
                    return data == null ? null : new PlaybackData(data);
                }
            }
        }

        public RecordingSettings GetRecordingSettings(T targetKey)
        {
            return new RecordingSettings(Path.Combine(_workingFolder, BuildTempAudioFileName()), true);
        }

        public string RegisterNewAudio(T targetKey, DateTime recordingDate, string recordedFilePath,
            IRecordingHistoryPolicy recordingPolicy)
        {
            byte[] audioData = File.ReadAllBytes(recordedFilePath);

            Guid audioId;
            if (recordingPolicy is AlwaysAddRecordingPolicy)
            {
                audioId = AddAudio(targetKey, recordingDate, audioData);
            }
            else
            {
                RecordedAudio latestAudio = GetLatestAudio(targetKey, true);
                if (latestAudio != null && recordingPolicy.OverrideLatestAudio(latestAudio.Recorded))
                {
                    audioId = latestAudio.AudioId;
                    UpdateAudio(audioId, recordingDate, audioData);
                }
                else
                {
                    audioId = AddAudio(targetKey, recordingDate, audioData);
                }
            }

            return audioId.ToString();
        }

        private Guid AddAudio(T targetKey, DateTime recordingDate, byte[] audioData)
        {
            var audioId = Guid.NewGuid();
            using (SqlCeConnection conn = new SqlCeConnection(_connectionString))
            {
                conn.Open();

                var cmd = new SqlCeCommand(string.Format(
@"INSERT RecordedAudio(AudioId, Recorded, TargetKey, TargetTypeId, RawData)
VALUES(@id, @recorded, @key, {0}, @data)", 
                    targetKey.TargetTypeId), conn);

                cmd.Parameters.AddWithValue("@id", audioId);
                cmd.Parameters.AddWithValue("@recorded", recordingDate);
                cmd.Parameters.AddWithValue("@key", targetKey.TargetKey);
                var parmData = cmd.Parameters.Add("@data", SqlDbType.Image);
                parmData.Value = audioData;

                int recordsCount = cmd.ExecuteNonQuery();
                if (recordsCount != 1)
                    throw new InvalidOperationException();
            }

            return audioId;
        }

        private void UpdateAudio(Guid audioId, DateTime recordingDate, byte[] audioData)
        {
            using (SqlCeConnection conn = new SqlCeConnection(_connectionString))
            {
                conn.Open();

                var cmd = new SqlCeCommand(
@"UPDATE RecordedAudio
SET Recorded = @recorded, RawData = @data
WHERE AudioId = @id", conn);
                cmd.Parameters.AddWithValue("@id", audioId);
                cmd.Parameters.AddWithValue("@recorded", recordingDate);
                var parmData = cmd.Parameters.Add("@data", SqlDbType.Image);
                parmData.Value = audioData;

                int recordsCount = cmd.ExecuteNonQuery();
                if (recordsCount != 1)
                    throw new InvalidOperationException();
            }
        }

        public bool DeleteAudios(T targetKey, IEnumerable<string> audioKeys)
        {
            Guid[] ids = audioKeys.Select(x => new Guid(x)).ToArray();
            if (ids.Length == 0)
                return true;

            string idString = string.Join(", ", ids.Select(x => string.Format("'{0}'", x)));
            using (SqlCeConnection conn = new SqlCeConnection(_connectionString))
            {
                conn.Open();

                var cmd = new SqlCeCommand(string.Format(
@"DELETE FROM RecordedAudio
WHERE AudioId IN ({0})", idString), conn);

                return cmd.ExecuteNonQuery() == ids.Length;
            }
        }

        public void DeleteTargetAudios(IEnumerable<T> targetKeys)
        {
            T[] keysArray = targetKeys.ToArray();
            if (keysArray.Length == 0)
                return;

            string keyString = string.Join(", ", 
                keysArray.Select(x => string.Format("'{0}'", PrepareSqlString(x.TargetKey))));
            using (SqlCeConnection conn = new SqlCeConnection(_connectionString))
            {
                conn.Open();

                var cmd = new SqlCeCommand(string.Format(
@"DELETE FROM RecordedAudio
WHERE TargetKey IN ({0}) AND TargetTypeId = {1}", keyString, keysArray[0].TargetTypeId), conn);

                cmd.ExecuteNonQuery();
            }
        }

        public bool MoveAudios<K>(T fromKey, K toKey, IEnumerable<string> audioKeys) where K : IRecordingTargetKey
        {
            Guid[] ids = audioKeys.Select(x => new Guid(x)).ToArray();
            if (ids.Length == 0)
                return true;

            IDatabaseTargetKey destinationKey = (IDatabaseTargetKey)toKey;
            string idString = string.Join(", ", ids.Select(x => string.Format("'{0}'", x)));
            using (SqlCeConnection conn = new SqlCeConnection(_connectionString))
            {
                conn.Open();

                var cmd = new SqlCeCommand(string.Format(
@"UPDATE RecordedAudio
SET TargetKey = @key, TargetTypeId = {0}
WHERE AudioId IN ({1})",
                    destinationKey.TargetTypeId, idString), conn);
                cmd.Parameters.AddWithValue("@key", destinationKey.TargetKey);

                return cmd.ExecuteNonQuery() == ids.Length;
            }
        }

        private string BuildTempAudioFileName()
        {
            return string.Format("{0:yyyy-MM-dd HH-mm-ss}.mp3", DateTime.Now);
        }

        private string PrepareSqlString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return input.Replace("'", "''");
        }
    }
}
