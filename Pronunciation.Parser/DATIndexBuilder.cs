using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Pronunciation.Parser
{
    class DATIndexBuilder
    {
        private readonly string _targetDATFile;
        private readonly string _targetIndexFile;

        public DATIndexBuilder(string targetDATFile, string targetIndexFile)
        {
            _targetDATFile = targetDATFile;
            _targetIndexFile = targetIndexFile;
        }

        public void WriteFiles(string sourceFolder)
        {
            var builder = new DATFileBuilder(_targetDATFile, false);
            var indexes = ProcessFolder(sourceFolder, builder);

            File.WriteAllLines(_targetIndexFile, indexes.Select(PrepareIndexString).OrderBy(x => x));
        }

        public void AppendFiles(string sourceFolder)
        {
            var builder = new DATFileBuilder(_targetDATFile, true);
            var indexes = ProcessFolder(sourceFolder, builder);

            var reader = new DATIndexReader(_targetDATFile, _targetIndexFile);
            var existingIndexes = reader.Index;
            foreach (var index in indexes)
            {
                existingIndexes[index.Key] = index.Value;
            }

            File.WriteAllLines(_targetIndexFile, existingIndexes.Select(PrepareIndexString).OrderBy(x => x));
        }

        private Dictionary<string, DataIndex> ProcessFolder(string sourceFolder, DATFileBuilder builder)
        {
            var indexes = new Dictionary<string, DataIndex>();
            foreach (string sourceFile in Directory.EnumerateFiles(sourceFolder, "*.mp3", SearchOption.TopDirectoryOnly))
            {
                string key = Path.GetFileNameWithoutExtension(sourceFile).ToLower();
                DataIndex value = builder.AppendEntity(key, File.ReadAllBytes(sourceFile));
                indexes.Add(key, value);
            }
            builder.Flush();

            return indexes;
        }

        private string PrepareIndexString(KeyValuePair<string, DataIndex> index)
        {
            return string.Format("{0}\t{1}", index.Key, index.Value.BuildKey());
        }
    }
}
