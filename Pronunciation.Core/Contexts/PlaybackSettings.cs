using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Contexts
{
    public class PlaybackSettings
    {
        public bool IsFilePath { get; private set; }
        public string PlaybackData { get; private set; }

        public PlaybackSettings(bool isFilePath, string playbackData)
        {
            IsFilePath = isFilePath;
            PlaybackData = playbackData;
        }
    }
}
