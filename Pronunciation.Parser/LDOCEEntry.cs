using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    class LDOCEEntry
    {
        public string Keyword;
        public EnglishVariant? Language;
        public bool IsDuplicate;
        public List<LDOCEEntryItem> Items;
    }

    class LDOCEEntryItem
    {
        public string ItemNumber;
        public string ItemKeyword;
        public DisplayName ItemTitle;
        public EnglishVariant? ItemLanguage;
        public List<LDOCEAlternativeSpelling> AlternativeSpellings;
        public string Transcription;
        public List<string> PartsOfSpeech;
        public string Notes;
        public string SoundFileUK;
        public string SoundFileUS;
        public string RawData;

        public bool HasAudio
        {
            get { return !string.IsNullOrEmpty(SoundFileUK) || !string.IsNullOrEmpty(SoundFileUS); }
        }

        public string KeywordWithNumber
        {
            get { 
                return string.Format("{0}{1}", ItemKeyword,
                    string.IsNullOrEmpty(ItemNumber) ? null : " " + ItemNumber); 
            }
        }
    }

    class LDOCEAlternativeSpelling
    {
        public string Keyword;
        public DisplayName Title;
        public EnglishVariant? Language;

        public LDOCEAlternativeSpelling(string keyword, DisplayName title, EnglishVariant? language)
        {
            Keyword = keyword;
            Title = title;
            Language = language;
        }
    }
}
