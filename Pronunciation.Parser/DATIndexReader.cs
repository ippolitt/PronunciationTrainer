using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Pronunciation.Parser
{
    class DATIndexReader : IDisposable
    {
        private readonly string _sourceDATFile;
        private readonly string _sourceIndexFile;
        private Lazy<Dictionary<string, DataIndex>> _index;
        private DATFileReader _reader;

        public DATIndexReader(string sourceDATFile, string sourceIndexFile)
        {
            _sourceDATFile = sourceDATFile;
            _sourceIndexFile = sourceIndexFile;
            _index = new Lazy<Dictionary<string, DataIndex>>(LoadIndex);
        }

        public Dictionary<string, DataIndex> Index
        {
            get { return _index.Value; }
        }

        public byte[] GetData(string soundKey)
        {
            if (_reader == null)
            {
                _reader = new DATFileReader(_sourceDATFile);
            }

            DataIndex entry;
            if (!_index.Value.TryGetValue(soundKey, out entry))
            {
                Console.WriteLine("Sound key '{0}' is missing in the index file '{0}'!", _sourceIndexFile);
                return null;
            }

            return _reader.GetData(entry);
        }

        public void ValidateIndex()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            using (var reader = new DATFileReader(_sourceDATFile))
            {
                foreach (var entry in _index.Value.OrderBy(x => x.Key))
                {
                    byte[] data = reader.GetData(entry.Value);
                    if (data.Length != entry.Value.Length)
                        throw new ArgumentException();
                }
            }

            watch.Stop();
        }

        private Dictionary<string, DataIndex> LoadIndex()
        {
            var indexes = new Dictionary<string, DataIndex>();
            foreach (string indexText in File.ReadAllLines(_sourceIndexFile))
            {
                if (string.IsNullOrWhiteSpace(indexText))
                    continue;

                var parts = indexText.Trim().Split('\t');
                indexes.Add(parts[0], DataIndex.Parse(parts[1]));
            }

            return indexes;
        }

        public void Dispose()
        {
            if (_reader != null)
            {
                _reader.Dispose();
            }
        }
    }
}
