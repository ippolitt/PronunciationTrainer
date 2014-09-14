using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pronunciation.Core.Utility
{
    public class DATFileReader
    {
        private readonly string _sourceFile;
        private const byte EntitySeparator = 0xAA;

        public DATFileReader(string sourceFile)
        {
            _sourceFile = sourceFile;
        }

        public void WarmUp()
        {
            // No errors on warmup (e.g. we may work without sounds)
            if (!File.Exists(_sourceFile))
                return;

            using (var stream = new FileStream(_sourceFile, FileMode.Open, FileAccess.Read))
            {
                stream.ReadByte();
            }
        }

        public byte[] GetData(long offset, long length)
        {
            if (length <= 0)
                return new byte[0];

            if (offset <= 0)
                throw new ArgumentException("Can't load data: the offset must be greater then zero!");

            using (var stream = new FileStream(_sourceFile, FileMode.Open, FileAccess.Read))
            {
                var buffer = new byte[length];
                stream.Seek(offset - 1, SeekOrigin.Begin);
                if (stream.ReadByte() != EntitySeparator)
                    throw new InvalidOperationException(BuildFileCorruptedMessage());

                stream.Read(buffer, 0, buffer.Length);
                if (stream.ReadByte() != EntitySeparator)
                    throw new InvalidOperationException(BuildFileCorruptedMessage());

                return buffer;
            }
        }

        private string BuildFileCorruptedMessage()
        {
            return string.Format(
                "Data file '{0}' is corrupted: signature bytes not found at the expected position!",
                _sourceFile);
        }
    }
}
