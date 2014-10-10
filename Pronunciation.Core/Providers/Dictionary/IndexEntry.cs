using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Providers.Dictionary
{
    public class IndexEntry
    {
        public string DisplayName { get; private set; }
        public int? UsageRank { get; private set; }
        public int? DictionaryId { get; private set; }
        public int? WordId { get; private set; }

        // Use static property to avoid storing reference in each instance of index entry
        public static IDictionaryProvider ActiveProvider { get; set; }

        private DictionaryWordInfo _word;

        public override string ToString()
        {
            return DisplayName;
        }

        public IndexEntry(string displayName, int? usageRank, int? dictionaryId, int wordId)
        {
            DisplayName = displayName;
            UsageRank = usageRank;
            DictionaryId = dictionaryId;
            WordId = wordId;
        }

        public IndexEntry(string displayName, int? usageRank, int? dictionaryId, DictionaryWordInfo word)
        {
            if (word == null)
                throw new ArgumentNullException();

            DisplayName = displayName;
            UsageRank = usageRank;
            DictionaryId = dictionaryId;
            _word = word;
        }

        public DictionaryWordInfo Word
        {
            get 
            {
                if (_word == null)
                {
                    if (WordId == null)
                        throw new ArgumentNullException();

                    _word = ActiveProvider.GetWordInfo(WordId.Value);
                }

                return _word;
            }
        }
    }
}
