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
        private readonly IFileLoader _fileLoader;
        private readonly string _connectionString;

        private SqlCeResultSet _wordsSet;
        private SqlCeResultSet _collocationsSet;
        private SqlCeConnection _connection;

        private int _wordPK = 0;
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

            cmdTable.CommandText = "Collocations";
            _collocationsSet = cmdTable.ExecuteResultSet(ResultSetOptions.Updatable);  
        }

        public void Dispose()
        {
            _wordsSet.Dispose();
            _collocationsSet.Dispose();

            _connection.Dispose();
        }

        public void InsertWord(WordDescription word, bool isLDOCEWord, string htmlIndex)
        {
            _wordPK++;

            // Word
            var wordRecord = _wordsSet.CreateRecord();
            wordRecord["WordId"] = _wordPK;
            wordRecord["Keyword"] = word.Text;
            wordRecord["HtmlIndex"] = htmlIndex;
            wordRecord["SoundKeyUK"] = word.SoundKeyUK;
            wordRecord["SoundKeyUS"] = word.SoundKeyUS;
            wordRecord["SoundIndexUK"] = word.SoundIndexUK;
            wordRecord["SoundIndexUS"] = word.SoundIndexUS;
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
                    collocationRecord["SoundKeyUK"] = collocation.SoundKeyUK;
                    collocationRecord["SoundKeyUS"] = collocation.SoundKeyUS;
                    collocationRecord["SoundIndexUK"] = collocation.SoundIndexUK;
                    collocationRecord["SoundIndexUS"] = collocation.SoundIndexUS;

                    _collocationsSet.Insert(collocationRecord);
                }
            }
        }
    }
}
