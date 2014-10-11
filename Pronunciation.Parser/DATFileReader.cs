using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pronunciation.Parser
{
    public class DATFileReader : IDisposable
    {
        private readonly string _sourceFile;
        private readonly FileStream _sourceStream;

        public DATFileReader(string sourceFile)
        {
            _sourceFile = sourceFile;
            _sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read);
        }

        public byte[] GetData(DataIndex index)
        {
            if (index.Length <= 0)
                return new byte[0];

            if (index.Offset <= 0)
                throw new ArgumentException("Can't load data: the offset must be greater then zero!");

            var buffer = new byte[index.Length];
            _sourceStream.Seek(index.Offset - 1, SeekOrigin.Begin);
            if (_sourceStream.ReadByte() != DATFileBuilder.EntitySeparator)
                throw new InvalidOperationException(BuildFileCorruptedMessage());

            _sourceStream.Read(buffer, 0, buffer.Length);
            if (_sourceStream.ReadByte() != DATFileBuilder.EntitySeparator)
                throw new InvalidOperationException(BuildFileCorruptedMessage());

            return buffer;
        }

        private string BuildFileCorruptedMessage()
        {
            return string.Format(
                "Data file '{0}' is corrupted: signature bytes not found at the expected position!",
                _sourceFile);
        }

        public void Dispose()
        {
            _sourceStream.Close();
        }
    }
}
