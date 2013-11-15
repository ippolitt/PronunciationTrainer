using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Web;
using Pronunciation.Core.Contexts;

namespace Pronunciation.Core.Providers
{
    public class LPDFileSystemProvider : LPDProvider, IDictionaryProvider
    {
        private readonly string _dictionaryFolder;
        private readonly CallScriptMethodHandler _scriptMethodInvoker;

        private const string _dictionaryFolderName = "Dic";
        private const string _indexFileName = "Index.txt";
        private const string GetAudioMethodName = "extGetAudioByKey";

        public delegate string CallScriptMethodHandler(string methodName, object[] methodArgs);

        public LPDFileSystemProvider(string baseFolder, string recordingsFolder, CallScriptMethodHandler scriptMethodInvoker)
            : base(baseFolder, recordingsFolder)
        {
            _scriptMethodInvoker = scriptMethodInvoker;

            _dictionaryFolder = Path.Combine(baseFolder, _dictionaryFolderName);
        }

        public PageInfo LoadArticlePage(string pageKey)
        {
            return new PageInfo(true, pageKey, BuildWordPath(pageKey));
        }

        public PageInfo InitPageFromUrl(Uri pageUrl)
        {
            string[] segments = pageUrl.Segments;
            string fileName = HttpUtility.UrlDecode(segments[segments.Length - 1]);

            // Check if URI ends with "Dic/[subfolder]/[page name]"
            bool isArticle = segments.Length >= 3
                ? string.Equals(segments[segments.Length - 3], _dictionaryFolderName + "/", StringComparison.OrdinalIgnoreCase)
                : false;

            string pageKey = Path.GetFileNameWithoutExtension(fileName);
            Uri fileUrl = isArticle ? BuildWordPath(pageKey) : BuildWordListPath(pageKey);
            if (!File.Exists(fileUrl.LocalPath))
                return null;

            return new PageInfo(isArticle, pageKey, fileUrl);
        }

        public PlaybackSettings GetReferenceAudio(string audioKey)
        {
            var base64Audio = _scriptMethodInvoker(GetAudioMethodName, new object[] { audioKey });
            if (string.IsNullOrEmpty(base64Audio))
                return null;

            return new PlaybackSettings(Convert.FromBase64String(base64Audio));
        }

        public PlaybackSettings GetAudioFromScriptData(string scriptData)
        {
            // The script should pass us base64 encoded mp3 file
            return string.IsNullOrEmpty(scriptData) ? null : new PlaybackSettings(Convert.FromBase64String(scriptData));
        }

        public List<IndexEntry> GetWords()
        {
            string indexFile = Path.Combine(BaseFolder, _indexFileName);
            if (!File.Exists(indexFile))
                return null;

            var words = new List<IndexEntry>();
            using (var reader = new StreamReader(indexFile, Encoding.UTF8))
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
    }
}
