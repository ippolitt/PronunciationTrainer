﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Web;
using Pronunciation.Core.Contexts;

using Pronunciation.Core.Providers.Recording;

namespace Pronunciation.Core.Providers.Dictionary
{
    public class FileSystemProvider : IDictionaryProvider
    {
        private readonly string _baseFolder;
        private readonly string _dictionaryFolder;
        private readonly CallScriptMethodHandler _scriptMethodInvoker;

        private const string DictionaryFolderName = "Dic";
        private const string IndexFileName = "Index.txt";
        private const string GetAudioMethodName = "extGetAudioByKey";

        public delegate string CallScriptMethodHandler(string methodName, object[] methodArgs);

        public FileSystemProvider(string baseFolder, CallScriptMethodHandler scriptMethodInvoker)
        {
            _baseFolder = baseFolder;
            _dictionaryFolder = Path.Combine(baseFolder, DictionaryFolderName);
            _scriptMethodInvoker = scriptMethodInvoker;
        }

        public void WarmUpSoundsStore()
        {
            // Do nothing
        }

        public ArticlePage PrepareArticlePage(IndexEntry index)
        {
            string articleKey = index.Word.ArticleKey;
            return new ArticlePage(articleKey, PreparePageUrl(articleKey), index);
        }

        // This method is called for all hyperlinks inside a dictionary page
        public PageInfo PrepareGenericPage(Uri pageUrl)
        {
            // Check if URI ends with "Dic/[subfolder]/[page name]"
            string[] segments = pageUrl.Segments;
            bool isArticle = segments.Length >= 3
                ? string.Equals(segments[segments.Length - 3], DictionaryFolderName + "/", StringComparison.OrdinalIgnoreCase)
                : false;

            if (isArticle)
            {
                string pageKey = Path.GetFileNameWithoutExtension(HttpUtility.UrlDecode(segments[segments.Length - 1]));
                return new ArticlePage(pageKey, PreparePageUrl(pageKey), null); 
            }
            else
            {
                return new PageInfo(pageUrl);
            }
        }

        public DictionarySoundInfo GetAudio(string soundKey)
        {
            if (string.IsNullOrEmpty(soundKey))
                return null;

            var base64Audio = _scriptMethodInvoker(GetAudioMethodName, new object[] { soundKey });
            if (string.IsNullOrEmpty(base64Audio))
                return null;

            return new DictionarySoundInfo(
                new PlaybackData(Convert.FromBase64String(base64Audio)), 
                IsUKAudio(soundKey));
        }

        public DictionarySoundInfo GetAudioFromScriptData(string soundKey, string scriptData)
        {
            if (string.IsNullOrEmpty(soundKey) || string.IsNullOrEmpty(scriptData))
                return null;

            // The script should pass us base64 encoded mp3 file
            return new DictionarySoundInfo(
                new PlaybackData(Convert.FromBase64String(scriptData)), 
                IsUKAudio(soundKey));
        }

        public List<IndexEntry> GetWordsIndex(int[] dictionaryIds)
        {
            string indexFile = Path.Combine(_baseFolder, IndexFileName);
            if (!File.Exists(indexFile))
                throw new Exception(string.Format("Dictionary index file '{0}' doesn't exist!", indexFile));

            var words = new List<IndexEntry>();
            bool checkDictionaryId = dictionaryIds != null && dictionaryIds.Length > 0;
            using (var reader = new StreamReader(indexFile, Encoding.UTF8))
            {
                while (!reader.EndOfStream)
                {
                    string[] data = reader.ReadLine().Split('\t');
                    if (data.Length != 6)
                        throw new InvalidOperationException("Index file is broken!");

                    int? dictionaryId = string.IsNullOrEmpty(data[5]) ? (int?)null : int.Parse(data[5]);
                    if (checkDictionaryId && !dictionaryIds.Contains(dictionaryId ?? 0))
                        continue;

                    words.Add(new IndexEntry(
                        data[0], 
                        string.IsNullOrEmpty(data[2]) ? (int?)null : int.Parse(data[2]),
                        dictionaryId, 
                        new DictionaryWordInfo(
                            data[1], 
                            data[3], 
                            data[4])));
                }
            }

            return words;
        }

        public DictionaryWordInfo GetWordInfo(int wordId)
        {
            throw new NotSupportedException();
        }

        public void UpdateFavoriteSound(int wordId, string favoriteSoundKey)
        {
            // do nothing
        }

        public List<int> GetWordsWithNotes()
        {
            return null;
        }

        private Uri PreparePageUrl(string pageKey)
        {
            Uri fileUrl = BuildWordPath(pageKey);
            if (!File.Exists(fileUrl.LocalPath))
                throw new ArgumentException(string.Format("Dictionary article '{0}' doesn't exist!", pageKey));

            return fileUrl;
        }

        private Uri BuildWordPath(string pageKey)
        {
            string fileName = pageKey.ToLower();

            return new Uri(Path.Combine(_dictionaryFolder,
                string.Format(@"{0}\{1}.html", BuildSubfolderName(fileName), fileName)));
        }

        private string BuildSubfolderName(string fileName)
        {
            return fileName.Substring(0, 1);
        }

        private bool IsUKAudio(string soundKey)
        {
            return soundKey.StartsWith("uk_") || soundKey.StartsWith("bre_") || soundKey.Contains("_bre_");
        }
    }
}
