using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pronunciation.Parser
{
    class FileLoader
    {
        private string _sourceFolder;

        private Dictionary<string, string> _cache;
        private string _cacheFolder;
        private bool _useCacheOnly;

        public FileLoader(string sourceFolder, string cacheFolder, bool useCacheOnly)
        {
            _sourceFolder = sourceFolder;
            _cacheFolder = cacheFolder;
            _useCacheOnly = useCacheOnly;
            _cache = new Dictionary<string, string>();
        }

        public bool FlushCache(string cacheFileName)
        {
            if (_useCacheOnly)
            {
                // No need to save the cache again over the same data
                _cache.Clear();
                return false;
            }

            if (!Directory.Exists(_cacheFolder))
            {
                Directory.CreateDirectory(_cacheFolder);
            }

            using (var dest = new StreamWriter(Path.Combine(_cacheFolder, cacheFileName)))
            {
                foreach (var item in _cache.OrderBy(x => x.Key))
                {
                    dest.WriteLine(item.Key);
                    dest.WriteLine(item.Value);
                }
            }

            _cache.Clear();
            return true;
        }

        public bool LoadCache(string cacheFileName)
        {
            _cache.Clear();

            var filePath = Path.Combine(_cacheFolder, cacheFileName);
            if (!File.Exists(filePath))
            {
                if (_useCacheOnly)
                    throw new ArgumentException();

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

            return true;
        }

        public string GetBase64Content(string fileName)
        {
            string result;
            if (_cache.TryGetValue(fileName, out result))
            {
                return result;
            }
            else
            {
                if (_useCacheOnly)
                    throw new ArgumentException();
            }

            string sourceFile = BuildFilePath(fileName);
            if (!File.Exists(sourceFile))
            {
                Console.WriteLine("\r\nMissing sound file '{0}'", fileName);
                return null;
                //throw new ArgumentException();
            }

            result = Convert.ToBase64String(File.ReadAllBytes(sourceFile));
            _cache[fileName] = result;

            return result;
        }

        public byte[] GetRawData(string fileName)
        {
            string base64 = GetBase64Content(fileName);
            if (string.IsNullOrEmpty(base64))
                return null;

            return Convert.FromBase64String(base64);
        }

        private string BuildFilePath(string fileName)
        {
            return Path.Combine(_sourceFolder, string.Format("{0}.mp3", fileName));
        }
    }
}
