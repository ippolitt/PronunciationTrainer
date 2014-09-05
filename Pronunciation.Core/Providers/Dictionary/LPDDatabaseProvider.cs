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
using Pronunciation.Core.Utility;

namespace Pronunciation.Core.Providers.Dictionary
{
    public class LPDDatabaseProvider : IDictionaryProvider
    {
        private class DataIndex
        {
            public long Offset;
            public long Length;

            public static DataIndex Parse(string dataIndex)
            {
                string[] index = dataIndex.Split('|');
                if (index.Length != 2)
                    throw new ArgumentException("Invalid format of data index!");

                return new DataIndex { Offset = long.Parse(index[0]), Length = long.Parse(index[1]) };
            }
        }

        private readonly string _baseFolder;
        private readonly string _lpdConnectionString;
        private readonly string _pageTemplate;
        private readonly string _indexFilePath;
        private readonly DATFileReader _audioReader;
        private readonly DATFileReader _htmlReader;

        private const string PageTemplateFileName = "PageTemplate.html";
        private const string IndexFileName = "Index.txt";
        private const string AudioDATFileName = "audio.dat";
        private const string HtmlDATFileName = "html.dat";

        public LPDDatabaseProvider(string baseFolder, string databaseFolder, string lpdConnectionString) 
        {
            _baseFolder = baseFolder;
            _lpdConnectionString = lpdConnectionString;
            _indexFilePath = Path.Combine(databaseFolder, IndexFileName);
            _pageTemplate = File.ReadAllText(Path.Combine(baseFolder, PageTemplateFileName));
            _audioReader = new DATFileReader(Path.Combine(databaseFolder, AudioDATFileName));
            _htmlReader = new DATFileReader(Path.Combine(databaseFolder, HtmlDATFileName));
        }

        public bool IsWordsIndexCached
        {
            get { return true; } //File.Exists(_indexFilePath);
        }

        public List<IndexEntry> GetWordsIndex(bool lpdDataOnly)
        {
            // Turn off index file - now it's loaded very fast even without it
            //if (IsWordsIndexCached)
            //    return GetCachedWordsIndex(lpdDataOnly);

            var index = new List<IndexEntry>();
            using (SqlCeConnection conn = new SqlCeConnection(_lpdConnectionString))
            {
                conn.Open();

                SqlCeCommand cmd = new SqlCeCommand();
                cmd.Connection = conn;

                // Load words
                var wordIndexes = new Dictionary<int, string>();
                cmd.CommandText =
@"SELECT WordId, Keyword, HtmlIndex, UsageRank, SoundIndexUK, SoundIndexUS, IsLDOCEWord  
FROM Words";
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string htmlIndex = reader["HtmlIndex"] as string;
                        if (!string.IsNullOrEmpty(htmlIndex))
                        {
                            wordIndexes[(int)reader["WordId"]] = htmlIndex;
                        }

                        index.Add(new IndexEntry(
                            htmlIndex,
                            reader["Keyword"] as string,
                            false,
                            reader["UsageRank"] as int?,
                            reader["SoundIndexUK"] as string,
                            reader["SoundIndexUS"] as string,
                            (reader["IsLDOCEWord"] as bool?) == true));
                    }
                }

                // Load collocations
                cmd.CommandText =
@"SELECT WordId, CollocationText, SoundIndexUK, SoundIndexUS  
FROM Collocations";
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string htmlIndex;
                        wordIndexes.TryGetValue((int)reader["WordId"], out htmlIndex);

                        index.Add(new IndexEntry(
                            htmlIndex,
                            reader["CollocationText"] as string,
                            true,
                            null,
                            reader["SoundIndexUK"] as string,
                            reader["SoundIndexUS"] as string,
                            false));
                    }
                }
            }

            //CacheWordsIndex(index);
            return lpdDataOnly ? index.Where(x => !x.IsLDOCEEntry).ToList() : index;
        }

        public ArticlePage PrepareArticlePage(string articleKey)
        {
            if (string.IsNullOrEmpty(articleKey))
                throw new ArgumentNullException();

            var index = DataIndex.Parse(articleKey);
            var data = _htmlReader.GetData(index.Offset, index.Length);

            return new ArticlePage(articleKey, BuildPageHtml(articleKey, Encoding.UTF8.GetString(data)));
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

            var index = DataIndex.Parse(soundKey);
            return new PlaybackData(_audioReader.GetData(index.Offset, index.Length));
        }

        public PlaybackData GetAudioFromScriptData(string scriptData)
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
