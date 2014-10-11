using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pronunciation.Parser
{
    class DATFileBuilder : IDATFileBuilder
    {
        private readonly string _targetFile;
        private readonly bool _isAppend;
        private readonly Dictionary<string, DataIndex> _indexes;
        private readonly List<byte> _flushBuffer;
        private bool _isInitialized;
        private long _flushBufferOffset;

        public const byte EntitySeparator = 0xAA;
        private const int AutoFlushLimit = 1000000; // 1MB

        public DATFileBuilder(string targetFile)
            : this(targetFile, false)
        {
        }

        public DATFileBuilder(string targetFile, bool isAppend)
        {
            _targetFile = targetFile;
            _isAppend = isAppend;
            _indexes = new Dictionary<string, DataIndex>();
            _flushBuffer = new List<byte>();
        }

        public DataIndex AppendEntity(string entityKey, byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentNullException();

            DataIndex index;
            if (_indexes.TryGetValue(entityKey, out index))
            {
                if (index.Length != data.Length)
                    throw new ArgumentException("Attempt to use the same data key for different data!");

                return index;
            }

            if (!_isInitialized)
            {
                if (_isAppend && File.Exists(_targetFile))
                {
                    _flushBufferOffset = new FileInfo(_targetFile).Length;
                }
                else
                {
                    File.WriteAllBytes(_targetFile, new byte[] { EntitySeparator });
                    _flushBufferOffset = 1;
                }
                _isInitialized = true;
            }

            if (_flushBuffer.Count > AutoFlushLimit)
            {
                Flush();
            }

            index = new DataIndex
            {
                Offset = _flushBuffer.Count + _flushBufferOffset,
                Length = data.Length
            };
            _indexes.Add(entityKey, index);

            _flushBuffer.AddRange(data);
            _flushBuffer.Add(EntitySeparator);

            return index;
        }

        public void Flush()
        {
            if (_flushBuffer.Count == 0)
                return;

            using (var stream = new FileStream(_targetFile, FileMode.Append))
            {
                stream.Write(_flushBuffer.ToArray(), 0, _flushBuffer.Count);
            }

            _flushBufferOffset += _flushBuffer.Count;
            _flushBuffer.Clear();
        }
    }
}
