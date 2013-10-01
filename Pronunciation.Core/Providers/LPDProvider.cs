﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Web;

namespace Pronunciation.Core.Providers
{
    public class LPDProvider
    {
        private readonly string _baseFolder;
        private readonly string _dictionaryFolder;
        private readonly string _recordingsFolder;

        private const string _baseFolderName = "LPD";
        private const string _dictionaryFolderName = "Dic";
        private const string _recordingsFolderName = "Recordings";
        private const string _indexFileName = "Index.txt";

        public LPDProvider(string appFolder)
        {
            _baseFolder = Path.Combine(appFolder, _baseFolderName);
            _dictionaryFolder = Path.Combine(_baseFolder, _dictionaryFolderName);
            _recordingsFolder = Path.Combine(_baseFolder, _recordingsFolderName);
        }

        public Uri BuildWordPath(string wordName)
        {
            string fileName = wordName.ToLower();

            return new Uri(Path.Combine(_dictionaryFolder,
                string.Format(@"{0}\{1}.html", BuildSubfolderName(fileName), fileName)));
        }

        public Uri BuildWordListPath(string listName)
        {
            return new Uri(Path.Combine(_baseFolder, string.Format(@"{0}.html", listName.ToLower())));
        }

        public string BuildRecordingFilePath(string wordName)
        {
            return Path.Combine(_recordingsFolder,
                string.Format(@"{0}\{1}.mp3", BuildSubfolderName(wordName), wordName));
        }

        public List<KeyTextPair<string>> GetWords()
        {
            string indexFile = Path.Combine(_baseFolder, _indexFileName);
            if (!File.Exists(indexFile))
                return null;

            var words = new List<KeyTextPair<string>>();
            using (var reader = new StreamReader(indexFile, Encoding.UTF8))
            {
                while (!reader.EndOfStream)
                {
                    string[] data = reader.ReadLine().Split('\t');
                    if (data.Length != 2)
                        throw new InvalidOperationException("Index file is broken!");

                    words.Add(new KeyTextPair<string>(data[1], data[0]));
                }
            }

            return words;
        }

        public List<KeyTextPair<string>> GetWordLists()
        {
            return new List<KeyTextPair<string>> { 
                new KeyTextPair<string>("1000", "Top 1000 words"),
                new KeyTextPair<string>("2000", "Top 2000 words"),
                new KeyTextPair<string>("3000", "Top 3000 words"),
                new KeyTextPair<string>("5000", "Top 5000 words"),
                new KeyTextPair<string>("7500", "Top 7500 words")
            };
        }

        public PageInfo GetPageInfo(Uri pageUrl)
        {
            string[] segments = pageUrl.Segments;
            string fileName = HttpUtility.UrlDecode(segments[segments.Length - 1]);

            // Check if URI ends with "Dic/[subfolder]/[page name]"
            bool isWord = segments.Length >= 3 
                ? string.Equals(segments[segments.Length - 3], _dictionaryFolderName + "/", StringComparison.OrdinalIgnoreCase)
                : false;
 
            return new PageInfo(isWord, Path.GetFileNameWithoutExtension(fileName));
        }

        private string BuildSubfolderName(string fileName)
        {
            return fileName.Substring(0, 1);
        }
    }
}