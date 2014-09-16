using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Contexts;

namespace Pronunciation.Core.Providers.Dictionary
{
    public class DictionarySoundInfo
    {
        public PlaybackData Data { get; private set; }
        public bool IsUKAudio { get; private set; }

        public DictionarySoundInfo(PlaybackData data, bool isUKAudio)
        {
            Data = data;
            IsUKAudio = isUKAudio;
        }
    }
}
