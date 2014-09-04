﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlServerCe;
using System.Data;

namespace Pronunciation.Parser
{
    class DatabaseUploader : IDisposable
    {
        private readonly IFileLoader _fileLoader;
        private readonly string _connectionString;
        private readonly Dictionary<string, int> _soundKeysLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private SqlCeResultSet _wordsSet;
        private SqlCeResultSet _soundsSet;
        private SqlCeResultSet _collocationsSet;
        private SqlCeConnection _connection;

        private int _wordPK = 0;
        private int _soundPK = 0;
        private int _collocationPK = 0;
        private StringBuilder _dbStats;

        public StringBuilder DbStats
        {
            get { return _dbStats; }
        }

        public DatabaseUploader(string connectionString, IFileLoader fileLoader)
        {
            _connectionString = connectionString;
            _fileLoader = fileLoader;
        }

        public void Open()
        {
            _dbStats = new StringBuilder();
            _connection = new SqlCeConnection(_connectionString);
            _connection.Open();

            SqlCeCommand cmdTable = new SqlCeCommand() 
            { 
                Connection = _connection,
                CommandType = CommandType.TableDirect
            };

            cmdTable.CommandText = "Words";
            _wordsSet = cmdTable.ExecuteResultSet(ResultSetOptions.Updatable);

            cmdTable.CommandText = "Sounds";
            _soundsSet = cmdTable.ExecuteResultSet(ResultSetOptions.Updatable);

            cmdTable.CommandText = "Collocations";
            _collocationsSet = cmdTable.ExecuteResultSet(ResultSetOptions.Updatable);  
        }

        public void Dispose()
        {
            _soundKeysLookup.Clear();

            _wordsSet.Dispose();
            _soundsSet.Dispose();
            _collocationsSet.Dispose();

            _connection.Dispose();
        }

        public void InsertWord(WordDescription word, bool isLDOCEWord, string htmlPage)
        {
            _wordPK++;

            // Sounds
            if (word.Sounds != null)
            {
                foreach (var soundInfo in word.Sounds)
                {
                    var soundKey = soundInfo.SoundKey;
                    if (_soundKeysLookup.ContainsKey(soundKey))
                    {
                        _dbStats.AppendFormat(
                            "Sound key '{0}' is also used by the word '{1}'\r\n", 
                            soundKey, word.Text);
                        continue;
                    }

                    _soundPK++;
                    _soundKeysLookup.Add(soundKey, _soundPK);

                    var soundRecord = _soundsSet.CreateRecord();
                    soundRecord["SoundId"] = _soundPK;
                    soundRecord["SoundKey"] = soundKey;
                    soundRecord["IsUKSound"] = soundInfo.IsUKSound;
                    soundRecord["SourceWordId"] = _wordPK;

                    byte[] rawData = _fileLoader.GetRawData(soundKey);
                    if (rawData != null)
                    {
                        soundRecord["RawData"] = rawData;
                    }

                    _soundsSet.Insert(soundRecord);
                }
            }

            // Word
            var wordRecord = _wordsSet.CreateRecord();
            wordRecord["WordId"] = _wordPK;
            wordRecord["Keyword"] = word.Text;
            wordRecord["HtmlPage"] = Encoding.UTF8.GetBytes(htmlPage);
            if (!string.IsNullOrEmpty(word.SoundKeyUK))
            {
                wordRecord["SoundIdUK"] = _soundKeysLookup[word.SoundKeyUK];
            }
            if (!string.IsNullOrEmpty(word.SoundKeyUS))
            {
                wordRecord["SoundIdUS"] = _soundKeysLookup[word.SoundKeyUS];
            }
            if (isLDOCEWord)
            {
                wordRecord["IsLDOCEWord"] = true;
            }

            // Word usage statistics
            if (word.UsageInfo != null)
            {
                if (word.UsageInfo.CombinedRank > 0)
                {
                    wordRecord["UsageRank"] = word.UsageInfo.CombinedRank;
                }
                if (word.UsageInfo.Ranks != null)
                {
                    var ranks = word.UsageInfo.Ranks;
                    if (!string.IsNullOrEmpty(ranks.LongmanSpoken))
                    {
                        wordRecord["RankLongmanS"] = ranks.LongmanSpoken;
                    }
                    if (!string.IsNullOrEmpty(ranks.LongmanWritten))
                    {
                        wordRecord["RankLongmanW"] = ranks.LongmanWritten;
                    }
                    if (ranks.Macmillan > 0)
                    {
                        wordRecord["RankMacmillan"] = ranks.Macmillan;
                    }
                    if (ranks.COCA > 0)
                    {
                        wordRecord["RankCOCA"] = ranks.COCA;
                    }
                }
            }

            _wordsSet.Insert(wordRecord);

            // Collocations
            if (word.Collocations != null)
            {
                foreach (var collocation in word.Collocations)
                {
                    _collocationPK++;

                    var collocationRecord = _collocationsSet.CreateRecord();
                    collocationRecord["CollocationId"] = _collocationPK;
                    collocationRecord["WordId"] = _wordPK;
                    collocationRecord["CollocationText"] = collocation.Text;
                    if (!string.IsNullOrEmpty(collocation.SoundKeyUK))
                    {
                        collocationRecord["SoundIdUK"] = _soundKeysLookup[collocation.SoundKeyUK];
                    }
                    if (!string.IsNullOrEmpty(collocation.SoundKeyUS))
                    {
                        collocationRecord["SoundIdUS"] = _soundKeysLookup[collocation.SoundKeyUS];
                    }

                    _collocationsSet.Insert(collocationRecord);
                }
            }
        }
    }
}
