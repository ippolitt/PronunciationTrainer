using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Providers.Dictionary
{
    public class IndexEntry
    {
        public int? WordId { get; private set; }
        public string ArticleKey { get; private set; }
        public string EntryText { get; private set; }
        public int? UsageRank { get; private set; }
        public string SoundKeyUK { get; private set; }
        public string SoundKeyUS { get; private set; }
        public int? DictionaryId { get; private set; }

        public override string ToString()
        {
            return EntryText;
        }

        public IndexEntry(string articleKey, string entryText, int? usageRank,
            string soundKeyUK, string soundKeyUS, int? dictionaryId, int? wordId)
        {
            ArticleKey = articleKey;
            EntryText = entryText;
            UsageRank = usageRank;
            SoundKeyUK = soundKeyUK;
            SoundKeyUS = soundKeyUS;
            DictionaryId = dictionaryId;
            WordId = wordId;
        }
    }
}
