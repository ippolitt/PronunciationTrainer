using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Contexts
{
    public class PlaybackData
    {
        public bool IsFilePath { get; private set; }
        public string FilePath { get; private set; }
        public byte[] RawData { get; private set; }

        public PlaybackData(string filePath)
        {
            IsFilePath = true;
            FilePath = filePath;
        }

        public PlaybackData(byte[] rawData)
        {
            IsFilePath = false;
            RawData = rawData;
        }
    }
}
