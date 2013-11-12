using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlServerCe;
using System.Data;

namespace Pronunciation.Parser
{
    class DatabaseUploader : IDisposable
    {
        private readonly FileLoader _fileLoader;
        private readonly string _connectionString;
        private readonly Dictionary<string, int> _soundKeysLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private SqlCeResultSet _wordsSet;
        private SqlCeResultSet _soundsSet;
        private SqlCeConnection _connection;

        private int _wordPK = 0;
        private int _soundPK = 0;

        public DatabaseUploader(string connectionString, FileLoader fileLoader)
        {
            _connectionString = connectionString;
            _fileLoader = fileLoader;
        }

        public void Open()
        {
            _connection = new SqlCeConnection(_connectionString);
            _connection.Open();

            SqlCeCommand cmdWords = new SqlCeCommand("Words", _connection);
            cmdWords.CommandType = CommandType.TableDirect;
            _wordsSet = cmdWords.ExecuteResultSet(ResultSetOptions.Updatable);

            SqlCeCommand cmdSounds = new SqlCeCommand("Sounds", _connection);
            cmdSounds.CommandType = CommandType.TableDirect;
            _soundsSet = cmdSounds.ExecuteResultSet(ResultSetOptions.Updatable);
        }

        public void Dispose()
        {
            _soundKeysLookup.Clear();
            _wordsSet.Dispose();
            _soundsSet.Dispose();
            _connection.Dispose();
        }

        public void InsertWord(string keyword, string htmlBody, HtmlBuilder.WordSounds soundsInfo)
        {
            if (soundsInfo.Sounds != null)
            {
                foreach (var soundInfo in soundsInfo.Sounds)
                {
                    var soundKey = soundInfo.SoundKey;
                    if (_soundKeysLookup.ContainsKey(soundKey))
                        continue;

                    _soundPK++;
                    _soundKeysLookup.Add(soundKey, _soundPK);

                    var soundRecord = _soundsSet.CreateRecord();
                    soundRecord["SoundId"] = _soundPK;
                    soundRecord["SoundKey"] = soundKey;

                    byte[] rawData = _fileLoader.GetRawData(soundKey);
                    if (rawData != null)
                    {
                        soundRecord["RawData"] = rawData;
                    }

                    _soundsSet.Insert(soundRecord);
                }
            }

            _wordPK++;

            var wordRecord = _wordsSet.CreateRecord();
            wordRecord["WordId"] = _wordPK;
            wordRecord["Keyword"] = keyword;
            wordRecord["HtmlPage"] = Encoding.UTF8.GetBytes(htmlBody);
            if (!string.IsNullOrEmpty(soundsInfo.SoundKeyUK))
            {
                wordRecord["SoundUk"] = _soundKeysLookup[soundsInfo.SoundKeyUK];
            }
            if (!string.IsNullOrEmpty(soundsInfo.SoundKeyUS))
            {
                wordRecord["SoundUs"] = _soundKeysLookup[soundsInfo.SoundKeyUS];
            }

            _wordsSet.Insert(wordRecord);
        }
    }
}
