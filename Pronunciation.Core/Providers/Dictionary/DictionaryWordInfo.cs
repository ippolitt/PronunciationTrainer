using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Providers.Dictionary
{
    public class DictionaryWordInfo
    {
        public string ArticleKey { get; private set; }
        public string SoundKeyUK { get; private set; }
        public string SoundKeyUS { get; private set; }

        public string FavoriteSoundKey { get; set; }
        public string FavoriteTranscription { get; set; }
        public string Notes { get; set; }
        public bool HasNotes { get; set; }

        public DictionaryWordInfo(string articleKey, string soundKeyUK, string soundKeyUS)
        {
            ArticleKey = articleKey;
            SoundKeyUK = soundKeyUK;
            SoundKeyUS = soundKeyUS;
        }
    }
}
