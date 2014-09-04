﻿using System;
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
    public class LPDDatabaseProvider : IDictionaryProvider
    {
        private readonly string _baseFolder;
        private readonly string _lpdConnectionString;
        private readonly string _pageTemplate;
        private readonly string _indexFilePath;

        private const string PageTemplateFileName = "PageTemplate.html";
        private const string IndexFileName = "IndexDB.txt";

        public LPDDatabaseProvider(string baseFolder, string lpdConnectionString) 
        {
            _baseFolder = baseFolder;
            _lpdConnectionString = lpdConnectionString;
            _indexFilePath = Path.Combine(baseFolder, IndexFileName);
            _pageTemplate = File.ReadAllText(Path.Combine(baseFolder, PageTemplateFileName));
        }

        public bool IsWordsIndexCached
        {
            get { return File.Exists(_indexFilePath); }
        }

        public List<IndexEntry> GetWordsIndex(bool lpdDataOnly)
        {
            if (IsWordsIndexCached)
                return GetCachedWordsIndex(lpdDataOnly);

            var index = new List<IndexEntry>();
            using (SqlCeConnection conn = new SqlCeConnection(_lpdConnectionString))
            {
                conn.Open();

                SqlCeCommand cmd = new SqlCeCommand();
                cmd.Connection = conn;

                // Load words
                cmd.CommandText =
@"SELECT w.WordId, w.Keyword AS Text, w.UsageRank, s1.SoundKey AS SoundKeyUK, s2.SoundKey AS SoundKeyUS, w.IsLDOCEWord  
FROM Words w 
    LEFT JOIN Sounds s1 ON w.SoundIdUK = s1.SoundId
    LEFT JOIN Sounds s2 ON w.SoundIdUS = s2.SoundId";
                AddIndexEntries(cmd, false, index);

                // Load collocations
                cmd.CommandText =
@"SELECT c.WordId, c.CollocationText AS Text, NULL AS UsageRank, s1.SoundKey AS SoundKeyUK, s2.SoundKey AS SoundKeyUS, NULL AS IsLDOCEWord  
FROM Collocations c 
    LEFT JOIN Sounds s1 ON c.SoundIdUK = s1.SoundId
    LEFT JOIN Sounds s2 ON c.SoundIdUS = s2.SoundId";
                AddIndexEntries(cmd, true, index);
            }

            CacheWordsIndex(index);
            return lpdDataOnly ? index.Where(x => !x.IsLDOCEEntry).ToList() : index;
        }

        public ArticlePage PrepareArticlePage(string articleKey)
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
                cmd.Parameters.AddWithValue("@wordId", int.Parse(articleKey));

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        throw new ArgumentException(string.Format(
                            "Dictionary article with Id={0} doesn't exist!", articleKey));
                    }

                    keyword = reader["Keyword"] as string;
                    pageBody = Encoding.UTF8.GetString(reader["HtmlPage"] as byte[]);
                }
            }

            return new ArticlePage(articleKey, BuildPageHtml(keyword, pageBody));
        }

        // May be called if a page contains a hyperlink to some external resource
        public PageInfo PrepareGenericPage(Uri pageUrl)
        {
            return new PageInfo(pageUrl);
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
                        reader["UsageRank"] as int?,
                        reader["SoundKeyUK"] as string,
                        reader["SoundKeyUS"] as string,
                        (reader["IsLDOCEWord"] as bool?) == true));
                }
            }
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
                bld.AppendLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}",
                    entry.EntryText, entry.ArticleKey, entry.IsCollocation ? 1 : 0, entry.UsageRank,
                    entry.SoundKeyUK, entry.SoundKeyUS, entry.IsLDOCEEntry ? 1 : 0));
            }

            File.WriteAllText(_indexFilePath, bld.ToString(), Encoding.UTF8);
        }

        private List<IndexEntry> GetCachedWordsIndex(bool lpdDataOnly)
        {
            var words = new List<IndexEntry>();
            using (var reader = new StreamReader(_indexFilePath, Encoding.UTF8))
            {
                while (!reader.EndOfStream)
                {
                    string[] data = reader.ReadLine().Split('\t');
                    if (data.Length != 7)
                        throw new InvalidOperationException("Index file is broken!");

                    bool isLDOCEEntry = (data[6] == "1");
                    if (lpdDataOnly && isLDOCEEntry)
                        continue;

                    words.Add(new IndexEntry(data[1], 
                        data[0], 
                        data[2] == "1" ? true : false,
                        string.IsNullOrEmpty(data[3]) ? (int?)null : int.Parse(data[3]), 
                        data[4], 
                        data[5],
                        isLDOCEEntry));
                }
            }

            return words;
        }
    }
}
