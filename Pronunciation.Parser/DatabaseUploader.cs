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
        private class WordIdInfo
        {
            public int WordId;
            public bool IsUsed;
            public int RecordPosition;
        }

        private readonly string _connectionString;
        private readonly IDATFileBuilder _htmlDATBuilder;
        private readonly SoundManager _soundManager;
        private readonly bool _preserveSounds;

        private int _maxWordId;
        private int _collocationId;
        private StringBuilder _dbStats;       
        private Dictionary<string, WordIdInfo> _wordIdMap;

        private SqlCeResultSet _wordsSet;
        private SqlCeResultSet _soundsSet;
        private SqlCeConnection _connection;

        public StringBuilder DbStats
        {
            get { return _dbStats; }
        }

        public DatabaseUploader(string connectionString, IDATFileBuilder htmlDATBuilder, SoundManager soundManager, 
            bool preserveSounds)
        {
            _connectionString = connectionString;
            _htmlDATBuilder = htmlDATBuilder;
            _soundManager = soundManager;
            _preserveSounds = preserveSounds;
        }

        public void Open()
        {
            _dbStats = new StringBuilder();
            _soundManager.Stats = _dbStats;
            _connection = new SqlCeConnection(_connectionString);
            _connection.Open();

            SqlCeCommand cmdTable = new SqlCeCommand() 
            { 
                Connection = _connection,
                CommandType = CommandType.TableDirect
            };

            cmdTable.CommandText = "DictionaryWord";
            _wordsSet = cmdTable.ExecuteResultSet(ResultSetOptions.Updatable | ResultSetOptions.Scrollable);

            if (!_preserveSounds)
            {
                cmdTable.CommandText = "DictionarySound";
                _soundsSet = cmdTable.ExecuteResultSet(ResultSetOptions.Updatable);
            }
        }

        public void StoreWord(WordDescription word, string html)
        {
            if (_wordIdMap == null)
            {
                _wordIdMap = BuildWordIdMap();
            }

            // Sounds
            if (!_preserveSounds && word.Sounds != null)
            {
                foreach (var soundInfo in word.Sounds)
                {
                    SoundManager.RegisteredSound registeredSound;
                    if (!_soundManager.RegisterSound(soundInfo, out registeredSound))
                        continue;

                    var soundRecord = _soundsSet.CreateRecord();
                    soundRecord["SoundKey"] = soundInfo.SoundKey;
                    soundRecord["IsUKSound"] = soundInfo.IsUKSound;
                    soundRecord["SourceFileId"] = registeredSound.DATFileId;
                    soundRecord["SoundIndex"] = registeredSound.SoundIndex;

                    _soundsSet.Insert(soundRecord);
                }
            }

            // Word
            SqlCeUpdatableRecord wordInsertRecord = null;
            DataRecordWrapper wordRecord;
            WordIdInfo wordInfo;
            int currentWordId;
            bool isUpdate = _wordIdMap.TryGetValue(word.Text, out wordInfo);
            if (isUpdate)
            {
                if (wordInfo.IsUsed)
                    throw new ArgumentException("The same word matched several times!");

                if (!_wordsSet.ReadAbsolute(wordInfo.RecordPosition))
                    throw new IndexOutOfRangeException("Failed to locate word record!");

                wordInfo.IsUsed = true;
                currentWordId = wordInfo.WordId;
                wordRecord = new DataRecordWrapper(_wordsSet); 
            }
            else
            {
                _maxWordId++;
                currentWordId = _maxWordId;

                wordInsertRecord = _wordsSet.CreateRecord();
                wordRecord = new DataRecordWrapper(wordInsertRecord);
                wordRecord["WordId"] = currentWordId;
                wordRecord["Keyword"] = word.Text;
            }

            var htmlIndex = _htmlDATBuilder.AppendEntity(word.Text, Encoding.UTF8.GetBytes(html));
            wordRecord["HtmlIndex"] = htmlIndex.BuildKey();
            wordRecord["SoundKeyUK"] = word.SoundKeyUK;
            wordRecord["SoundKeyUS"] = word.SoundKeyUS;
            wordRecord["DictionaryId"] = word.DictionaryId;
            wordRecord["IsCollocation"] = word.IsCollocation ? true : (bool?)null;

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

            if (isUpdate)
            {
                _wordsSet.Update();
            }
            else
            {
                _wordsSet.Insert(wordInsertRecord);
                _wordIdMap.Add(word.Text, new WordIdInfo { WordId = currentWordId, RecordPosition = _wordIdMap.Count, IsUsed = true });
            }
        }

        private Dictionary<string, WordIdInfo> BuildWordIdMap()
        {
            var wordIdMap = new Dictionary<string, WordIdInfo>();
            int position = 0;
            bool isRecord = _wordsSet.ReadFirst();
            while (isRecord)
            {
                var wordId = (int)_wordsSet["WordId"];
                if (wordId > _maxWordId)
                {
                    _maxWordId = wordId;
                }
                wordIdMap.Add((string)_wordsSet["Keyword"], new WordIdInfo { WordId = wordId, RecordPosition = position });
                position++;

                isRecord = _wordsSet.Read();
            }

            return wordIdMap;
        }

        public int DeleteExtraWords()
        {
            if (_wordIdMap == null || _wordIdMap.Count == 0)
                return 0;

            int deleteCount = 0;
            var idsToDelete = new HashSet<int>(_wordIdMap.Values.Where(x => !x.IsUsed).Select(x => x.WordId));
            bool isRecord = _wordsSet.ReadFirst();
            while (isRecord)
            {
                if (idsToDelete.Contains((int)_wordsSet["WordId"]))
                {
                    var keyword = (string)_wordsSet["Keyword"];
                    try
                    {
                        _wordsSet.Delete();
                        deleteCount++;
                        _dbStats.AppendFormat("Deleted extra word '{0}'\r\n", keyword);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("\r\nFAILED to delete extra word: " + ex.ToString());
                        _dbStats.AppendFormat("FAILED to delete extra word '{0}': {1}\r\n", keyword, ex);
                    }
                }

                isRecord = _wordsSet.Read();
            }

            // The map doesn't have a sense anymore because all indexes have changed
            _wordIdMap = null;
            _dbStats.AppendFormat("Totally deleted '{0}' extra words\r\n", deleteCount);

            return deleteCount;
        }

        public void FinishUpload()
        {
            _htmlDATBuilder.Flush();
            _soundManager.Flush();

            _dbStats.AppendFormat("Totally '{0}' sound keys have been remapped.\r\n", _soundManager.ReusedKeysCount);
        }

        public void Dispose()
        {
            _wordIdMap = null;

            _wordsSet.Dispose();
            if (_soundsSet != null)
            {
                _soundsSet.Dispose();
            }

            _connection.Dispose();
        }
    }
}
