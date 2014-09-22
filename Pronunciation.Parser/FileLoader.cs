using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pronunciation.Parser
{
    class FileLoader : IFileLoader
    {
        private readonly string _sourceFolderLPD;
        private readonly string _sourceFolderLDOCE;
        private readonly string _sourceFolderMW;
        private readonly Dictionary<string, string> _cache;
        private readonly string _cacheFolder;
        private readonly bool _useCacheOnly;

        private const string LDOCEFolderUK = "SoundsUK";
        private const string LDOCEFolderUS = "SoundsUS";

        private string _currentCacheFile;

        public FileLoader(string sourceFolderLPD, string sourceFolderLDOCE, string sourceFolderMW, 
            string cacheFolder, bool useCacheOnly)
        {
            _sourceFolderLPD = sourceFolderLPD;
            _sourceFolderLDOCE = sourceFolderLDOCE;
            _sourceFolderMW = sourceFolderMW;
            _cacheFolder = cacheFolder;
            _useCacheOnly = useCacheOnly;
            _cache = new Dictionary<string, string>();
        }

        public bool FlushCache()
        {
            if (_useCacheOnly || string.IsNullOrEmpty(_currentCacheFile))
                return false;

            if (!Directory.Exists(_cacheFolder))
            {
                Directory.CreateDirectory(_cacheFolder);
            }

            using (var dest = new StreamWriter(Path.Combine(_cacheFolder, _currentCacheFile), false))
            {
                foreach (var item in _cache.OrderBy(x => x.Key))
                {
                    dest.WriteLine(item.Key);
                    dest.WriteLine(item.Value);
                }
            }

            return true;
        }

        public bool LoadCache(string cacheKey)
        {
            ClearCache();

            string cacheFileName = string.Format("{0}.txt", cacheKey);
            var filePath = Path.Combine(_cacheFolder, cacheFileName);
            if (!File.Exists(filePath))
            {
                if (_useCacheOnly)
                    throw new ArgumentException();

                _currentCacheFile = cacheFileName;
                return false;
            }

            using (var source = new StreamReader(filePath))
            {
                while (!source.EndOfStream)
                {
                    var key = source.ReadLine();
                    var value = source.ReadLine();
                    if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value) || key.Length > value.Length)
                        throw new ArgumentException();

                    _cache.Add(key, value);
                }
            }

            _currentCacheFile = cacheFileName;
            return true;
        }

        public void ClearCache()
        {
            _cache.Clear();
            _currentCacheFile = null;
        }

        public string GetBase64Content(string fileKey)
        {
            string result;
            if (_cache.TryGetValue(fileKey, out result))
                return result;
            
            if (_useCacheOnly)
                throw new ArgumentException();

            byte[] rawData = GetFileContent(fileKey);
            if (rawData == null)
                return null;

            result = Convert.ToBase64String(rawData);
            _cache[fileKey] = result;

            return result;
        }

        public byte[] GetRawData(string fileKey)
        {
            string base64;
            if (_cache.TryGetValue(fileKey, out base64))
                return string.IsNullOrEmpty(base64) ? null : Convert.FromBase64String(base64);

            if (_useCacheOnly)
                throw new ArgumentException();

            byte[] rawData = GetFileContent(fileKey);
            if (rawData == null)
                return null;

            _cache[fileKey] = Convert.ToBase64String(rawData);
            return rawData;
        }

        private byte[] GetFileContent(string fileKey)
        {
            string sourceFile;
            if (fileKey.StartsWith(SoundManager.LDOCE_SoundKeyPrefix))
            {
                string fileName = string.Format("{0}.mp3", fileKey.Remove(0, SoundManager.LDOCE_SoundKeyPrefix.Length));
                sourceFile = Path.Combine(_sourceFolderLDOCE, LDOCEFolderUK, fileName);
                if (!File.Exists(sourceFile))
                {
                    sourceFile = Path.Combine(_sourceFolderLDOCE, LDOCEFolderUS, fileName);
                }
            }
            else if (fileKey.StartsWith(SoundManager.MW_SoundKeyPrefix))
            {
                sourceFile = Path.Combine(_sourceFolderMW, string.Format("{0}.mp3", 
                    fileKey.Remove(0, SoundManager.MW_SoundKeyPrefix.Length)));
            }
            else
            {
                sourceFile = Path.Combine(_sourceFolderLPD, string.Format("{0}.mp3", fileKey));
            }

            if (!File.Exists(sourceFile))
            {
                Console.WriteLine("\r\nMissing sound file '{0}'", fileKey);
                //FileLoaderMock.PrepareMissingFile(fileKey);
                return null;
                //throw new ArgumentException();
            }

            return File.ReadAllBytes(sourceFile);
        }
    }
}
