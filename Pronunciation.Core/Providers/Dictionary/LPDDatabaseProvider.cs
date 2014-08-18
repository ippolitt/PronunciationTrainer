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

namespace Pronunciation.Core.Providers.Dictionary
{
    public class LPDDatabaseProvider : LPDProviderBase, IDictionaryProvider
    {
        private readonly string _lpdConnectionString;
        private readonly string _pageTemplate;
        private readonly string _indexFilePath;

        private const string WorkingFolderName = "LPD";
        private const string PageTemplateFileName = "PageTemplate.html";
        private const string IndexFileName = "Index.txt";

        public LPDDatabaseProvider(string baseFolder, string lpdConnectionString) 
            : base(baseFolder)
        {
            _lpdConnectionString = lpdConnectionString;
            _indexFilePath = Path.Combine(baseFolder, IndexFileName);
            _pageTemplate = File.ReadAllText(Path.Combine(baseFolder, PageTemplateFileName));
        }

        public bool IsWordsIndexCached
        {
            get { return File.Exists(_indexFilePath); }
        }

        public List<IndexEntry> GetWordsIndex()
        {
            if (IsWordsIndexCached)
                return GetCachedWordsIndex();

            var index = new List<IndexEntry>();
            using (SqlCeConnection conn = new SqlCeConnection(_lpdConnectionString))
            {
                conn.Open();

                SqlCeCommand cmd = new SqlCeCommand();
                cmd.Connection = conn;

                // Load words
                cmd.CommandText =
@"SELECT w.WordId, w.Keyword AS Text, s1.SoundKey AS SoundKeyUK, s2.SoundKey AS SoundKeyUS  
FROM Words w 
    LEFT JOIN Sounds s1 ON w.SoundIdUK = s1.SoundId
    LEFT JOIN Sounds s2 ON w.SoundIdUS = s2.SoundId";
                AddIndexEntries(cmd, false, index);

                // Load collocations
                cmd.CommandText =
@"SELECT c.WordId, c.CollocationText AS Text, s1.SoundKey AS SoundKeyUK, s2.SoundKey AS SoundKeyUS  
FROM Collocations c 
    LEFT JOIN Sounds s1 ON c.SoundIdUK = s1.SoundId
    LEFT JOIN Sounds s2 ON c.SoundIdUS = s2.SoundId";
                AddIndexEntries(cmd, true, index);
            }

            CacheWordsIndex(index);
            return index;
        }

        public PageInfo LoadArticlePage(string pageKey)
        {
            string pageBody = null;
            string keyword = null;
            using (SqlCeConnection conn = new SqlCeConnection(_lpdConnectionString))
            {
                conn.Open();

                SqlCeCommand cmd = new SqlCeCommand(
@"SELECT Keyword, HtmlPage
FROM Words
WHERE WordId = @wordId", conn);
                cmd.Parameters.AddWithValue("@wordId", int.Parse(pageKey));

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        throw new ArgumentException(string.Format(
                            "Dictionary entry with Id={0} doesn't exist!", pageKey));
                    }

                    keyword = reader["Keyword"] as string;
                    pageBody = Encoding.UTF8.GetString(reader["HtmlPage"] as byte[]);
                }
            }

            return new PageInfo(true, pageKey, BuildPageHtml(keyword, pageBody));
        }

        public PageInfo InitPageFromUrl(Uri pageUrl)
        {
            throw new NotSupportedException();
        }

        public PlaybackData GetAudio(string soundKey)
        {
            if (string.IsNullOrEmpty(soundKey))
                return null;

            using (SqlCeConnection conn = new SqlCeConnection(_lpdConnectionString))
            {
                conn.Open();

                SqlCeCommand cmd = new SqlCeCommand(
@"SELECT RawData
FROM Sounds
WHERE SoundKey = @soundKey", conn);
                cmd.Parameters.AddWithValue("@soundKey", soundKey);

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        throw new ArgumentException(string.Format(
                            "Audio with key='{0}' doesn't exist!", soundKey));
                    }

                    return new PlaybackData(reader["RawData"] as byte[]);
                }
            }
        }

        public PlaybackData GetAudioFromScriptData(string scriptData)
        {
            throw new NotSupportedException();
        }

        private void AddIndexEntries(SqlCeCommand command, bool isCollocation, List<IndexEntry> index)
        {
            using (SqlCeDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    index.Add(new IndexEntry(
                        ((int)reader["WordId"]).ToString(),
                        reader["Text"] as string,
                        isCollocation,
                        reader["SoundKeyUK"] as string,
                        reader["SoundKeyUS"] as string));
                }
            }
        }

        private string BuildPageHtml(string title, string pageBody)
        {
            // Don't use AbsolutePath because it replaces spaces with "%20" symbol 
            string pageBase = new Uri(BaseFolder).LocalPath;
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
                    entry.Text, entry.PageKey, entry.IsCollocation ? 1 : 0,
                    entry.SoundKeyUK, entry.SoundKeyUS));
            }

            File.WriteAllText(_indexFilePath, bld.ToString(), Encoding.UTF8);
        }

        private List<IndexEntry> GetCachedWordsIndex()
        {
            var words = new List<IndexEntry>();
            using (var reader = new StreamReader(_indexFilePath, Encoding.UTF8))
            {
                while (!reader.EndOfStream)
                {
                    string[] data = reader.ReadLine().Split('\t');
                    if (data.Length != 5)
                        throw new InvalidOperationException("Index file is broken!");

                    words.Add(new IndexEntry(data[1], data[0], data[2] == "1" ? true : false, data[3], data[4]));
                }
            }

            return words;
        }
    }
}
