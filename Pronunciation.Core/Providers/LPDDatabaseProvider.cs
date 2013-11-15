using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlServerCe;
using Pronunciation.Core.Contexts;
using System.IO;
using System.Web;

namespace Pronunciation.Core.Providers
{
    public class LPDDatabaseProvider : LPDProvider, IDictionaryProvider
    {
        private readonly string _connectionString;
        private readonly string _pageTemplate;

        private const string PageTemplateFileName = "PageTemplate.html";

        public LPDDatabaseProvider(string baseFolder, string recordingsFolder, string connectionString)
            : base(baseFolder, recordingsFolder)
        {
            _connectionString = connectionString;
            _pageTemplate = File.ReadAllText(Path.Combine(baseFolder, PageTemplateFileName));
        }

        public List<IndexEntry> GetWords()
        {
            var index = new List<IndexEntry>();
            using (SqlCeConnection conn = new SqlCeConnection(_connectionString))
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

            return index;
        }

        public PageInfo LoadArticlePage(string pageKey)
        {
            string pageBody = null;
            string keyword = null;
            using (SqlCeConnection conn = new SqlCeConnection(_connectionString))
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

        // We can only parse URLs of the word lists
        public PageInfo InitPageFromUrl(Uri pageUrl)
        {
            string[] segments = pageUrl.Segments;
            string pageKey = Path.GetFileNameWithoutExtension(HttpUtility.UrlDecode(segments[segments.Length - 1]));

            Uri listUrl = BuildWordListPath(pageKey);
            if (!File.Exists(listUrl.LocalPath))
                return null;

            return new PageInfo(false, pageKey, listUrl);
        }

        public PlaybackSettings GetReferenceAudio(string audioKey)
        {
            if (string.IsNullOrEmpty(audioKey))
                return null;

            using (SqlCeConnection conn = new SqlCeConnection(_connectionString))
            {
                conn.Open();

                SqlCeCommand cmd = new SqlCeCommand(
@"SELECT RawData
FROM Sounds
WHERE SoundKey = @soundKey", conn);
                cmd.Parameters.AddWithValue("@soundKey", audioKey);

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        throw new ArgumentException(string.Format(
                            "Audio with key='{0}' doesn't exist!", audioKey));
                    }

                    return new PlaybackSettings(reader["RawData"] as byte[]);
                }
            }
        }

        public PlaybackSettings GetAudioFromScriptData(string scriptData)
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
            string pageBase = new Uri(BaseFolder).AbsolutePath;
            if (!pageBase.EndsWith("/"))
            {
                pageBase += "/";
            }
            return string.Format(_pageTemplate, title, pageBase, pageBody);
        }
    }
}
