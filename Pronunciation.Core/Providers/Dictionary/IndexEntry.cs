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
        public bool? HasMultiplePronunciations { get; private set; }

        public string AlternativeName { get; set; }

        // Use static property to avoid storing reference in each instance of index entry
        public static IDictionaryProvider ActiveProvider { get; set; }
        
        private DictionaryWordInfo _word;

        public override string ToString()
        {
            return DisplayName;
        }

        private IndexEntry(string displayName, int? usageRank, int? dictionaryId)
        {
            DisplayName = displayName;
            UsageRank = usageRank;
            DictionaryId = dictionaryId;
        }

        public IndexEntry(string displayName, int? usageRank, int? dictionaryId,  bool? hasMultiplePronunciations, int wordId)
            : this (displayName, usageRank, dictionaryId)
        {
            HasMultiplePronunciations = hasMultiplePronunciations;
            WordId = wordId;
        }

        public IndexEntry(string displayName, int? usageRank, int? dictionaryId, DictionaryWordInfo word)
            : this(displayName, usageRank, dictionaryId)
        {
            if (word == null)
                throw new ArgumentNullException();

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
