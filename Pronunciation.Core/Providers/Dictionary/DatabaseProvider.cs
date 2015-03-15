using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlServerCe;
using Pronunciation.Core.Contexts;
using System.IO;
using System.Web;

using Pronunciation.Core.Providers.Recording;
using Pronunciation.Core.Database;
using Pronunciation.Core.Utility;

namespace Pronunciation.Core.Providers.Dictionary
{
    public class DatabaseProvider : IDictionaryProvider
    {
        private readonly string _baseFolder;
        private readonly string _connectionString;
        private readonly string _pageTemplate;
        private readonly string _indexFilePath;
        private readonly DictionaryDATReader _datReader;

        private const string PageTemplateFileName = "PageTemplate.html";
        private const string IndexFileName = "Index.txt";

        public DatabaseProvider(string baseFolder, string databaseFolder, string connectionString)
        {
            _baseFolder = baseFolder;
            _connectionString = connectionString;
            _indexFilePath = Path.Combine(databaseFolder, IndexFileName);
            _pageTemplate = File.ReadAllText(Path.Combine(baseFolder, PageTemplateFileName));
            _datReader = new DictionaryDATReader(databaseFolder);
        }

        public void WarmUpSoundsStore()
        {
            _datReader.WarmUp();
        }

        public List<IndexEntry> GetWordsIndex(int[] dictionaryIds)
        {
            if (File.Exists(_indexFilePath))
                return GetCachedWordsIndex(dictionaryIds);

            // We must laod and cache all data (even if we return a subset)
            List<IndexEntry> index = LoadWordsIndex();
            CacheWordsIndex(index);

            if (dictionaryIds == null || dictionaryIds.Length <= 0)
                return index;

            return index.Where(x => dictionaryIds.Contains(x.DictionaryId ?? 0)).ToList();
        }

        private List<IndexEntry> LoadWordsIndex()
        {
            var index = new List<IndexEntry>();
            using (SqlCeConnection conn = new SqlCeConnection(_connectionString))
            {
                conn.Open();

                SqlCeCommand cmd = new SqlCeCommand();
                cmd.Connection = conn;

                // Load words
                cmd.CommandText =
@"SELECT WordId, Keyword, UsageRank, DictionaryId, HasMultiplePronunciations  
FROM DictionaryWord";
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        index.Add(new IndexEntry(
                            reader["Keyword"] as string,
                            reader["UsageRank"] as int?,
                            reader["DictionaryId"] as int?,
                            reader["HasMultiplePronunciations"] as bool?,
                            (int)reader["WordId"]));
                    }
                }
            }

            return index;
        }

        public ArticlePage PrepareArticlePage(IndexEntry index)
        {
            string articleKey = index.Word.ArticleKey;
            var data = _datReader.GetHtmlData(articleKey);

            return new ArticlePage(articleKey,
                BuildPageHtml(articleKey, Encoding.UTF8.GetString(data)),
                index);
        }

        // May be called if a page contains a hyperlink to some external resource
        public PageInfo PrepareGenericPage(Uri pageUrl)
        {
            return new PageInfo(pageUrl);
        }

        public DictionarySoundInfo GetAudio(string soundKey)
        {
            if (string.IsNullOrEmpty(soundKey))
                return null;

            string soundIndex;
            bool isUKAudio;
            int? sourceFileId;
            using (SqlCeConnection conn = new SqlCeConnection(_connectionString))
            {
                conn.Open();

                SqlCeCommand cmd = new SqlCeCommand(
@"SELECT IsUKSound, SoundIndex, SourceFileId
FROM DictionarySound
WHERE SoundKey = @soundKey", conn);
                cmd.Parameters.AddWithValue("@soundKey", soundKey);

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        throw new ArgumentException(string.Format(
                            "Audio with key='{0}' doesn't exist!", soundKey));
                    }

                    isUKAudio = (bool)reader["IsUKSound"];
                    soundIndex = (string)reader["SoundIndex"];
                    sourceFileId = reader["SourceFileId"] as int?;
                }
            }

            var data = _datReader.GetAudioData(sourceFileId, soundIndex);
            return new DictionarySoundInfo(new PlaybackData(data), isUKAudio);
        }

        public DictionaryWordInfo GetWordInfo(int wordId)
        {
            DictionaryWordInfo word = null;
            using (SqlCeConnection conn = new SqlCeConnection(_connectionString))
            {
                conn.Open();

                SqlCeCommand cmd = new SqlCeCommand();
                cmd.Connection = conn;
                cmd.CommandText =
@"SELECT HtmlIndex, SoundKeyUK, SoundKeyUS, FavoriteSoundKey, FavoriteTranscription, Notes, HasNotes
FROM DictionaryWord
WHERE WordId = @wordId";
                cmd.Parameters.AddWithValue("@wordId", wordId);

                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        word = new DictionaryWordInfo(
                            reader["HtmlIndex"] as string,
                            reader["SoundKeyUK"] as string,
                            reader["SoundKeyUS"] as string);
                        word.FavoriteSoundKey = reader["FavoriteSoundKey"] as string;
                        word.FavoriteTranscription = reader["FavoriteTranscription"] as string;
                        word.Notes = reader["Notes"] as string;
                        word.HasNotes = (reader["HasNotes"] as bool?) == true;

                        break;
                    }
                }
            }

            return word;
        }

        public void UpdateFavoriteSound(int wordId, string favoriteSoundKey)
        {
            using (SqlCeConnection conn = new SqlCeConnection(_connectionString))
            {
                conn.Open();

                SqlCeCommand cmd = new SqlCeCommand();
                cmd.Connection = conn;
                cmd.CommandText =
@"UPDATE DictionaryWord
SET FavoriteSoundKey = @soundKey
WHERE WordId = @wordId";
                cmd.Parameters.AddWithValue("@wordId", wordId);
                var parm = cmd.Parameters.Add("@soundKey", System.Data.SqlDbType.NVarChar);
                parm.Value = string.IsNullOrEmpty(favoriteSoundKey) ? (object)DBNull.Value : (object)favoriteSoundKey;

                if (cmd.ExecuteNonQuery() <= 0)
                    throw new ArgumentException("Favorite sound for the word hasn't been updated!");
            }
        }

        public List<int> GetWordsWithNotes()
        {
            List<int> wordIds = new List<int>();
            using (SqlCeConnection conn = new SqlCeConnection(_connectionString))
            {
                conn.Open();

                SqlCeCommand cmd = new SqlCeCommand();
                cmd.Connection = conn;
                cmd.CommandText =
@"SELECT WordId
FROM DictionaryWord
WHERE HasNotes = 'True'";

                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        wordIds.Add((int)reader["WordId"]);
                    }
                }
            }

            return wordIds;
        }

        public DictionarySoundInfo GetAudioFromScriptData(string soundKey, string scriptData)
        {
            throw new NotSupportedException();
        }

        private string BuildPageHtml(string title, string pageBody)
        {
            // Don't use AbsolutePath because it replaces spaces with "%20" symbol 
            string pageBase = new Uri(_baseFolder).LocalPath;
            if (!pageBase.EndsWith("/"))
            {
                pageBase += "/";
            }
            return string.Format(_pageTemplate, title, pageBase, pageBody);
        }

        private void CacheWordsIndex(List<IndexEntry> index)
        {
            var bld = new StringBuilder();
            foreach (var entry in index)
            {
                bld.AppendLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}",
                    entry.DisplayName, entry.UsageRank, entry.WordId, entry.DictionaryId, entry.HasMultiplePronunciations));
            }

            File.WriteAllText(_indexFilePath, bld.ToString(), Encoding.UTF8);
        }

        private List<IndexEntry> GetCachedWordsIndex(int[] dictionaryIds)
        {
            var words = new List<IndexEntry>();
            bool checkDictionaryId = dictionaryIds != null && dictionaryIds.Length > 0;

            using (var reader = new StreamReader(_indexFilePath, Encoding.UTF8))
            {
                while (!reader.EndOfStream)
                {
                    string[] data = reader.ReadLine().Split('\t');
                    if (data.Length != 5)
                        throw new InvalidOperationException("Index file is broken!");

                    int? dictionaryId = string.IsNullOrEmpty(data[3]) ? (int?)null : int.Parse(data[3]);
                    if (checkDictionaryId && !dictionaryIds.Contains(dictionaryId ?? 0))
                        continue;

                    words.Add(new IndexEntry(data[0], 
                        string.IsNullOrEmpty(data[1]) ? (int?)null : int.Parse(data[1]),
                        dictionaryId, 
                        string.IsNullOrEmpty(data[4]) ? (bool?)null : bool.Parse(data[4]),
                        int.Parse(data[2])));
                }
            }

            return words;
        }
    }
}
