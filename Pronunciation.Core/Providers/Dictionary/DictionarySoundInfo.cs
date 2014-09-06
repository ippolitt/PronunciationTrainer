using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Contexts;

namespace Pronunciation.Core.Providers.Dictionary
{
    public class DictionarySoundInfo
    {
        public string SoundText { get; private set; }
        public PlaybackData Data { get; private set; }
        public bool IsUKAudio { get; private set; }

        public DictionarySoundInfo(PlaybackData data, bool isUKAudio)
            : this(data, isUKAudio, null)
        {
        }

        public DictionarySoundInfo(PlaybackData data, bool isUKAudio, string soundText)
        {
            Data = data;
            IsUKAudio = isUKAudio;
            SoundText = soundText;
        }
    }
}
