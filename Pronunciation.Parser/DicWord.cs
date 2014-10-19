using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    class DicWord
    {
        private string _keyword;
        private string _title;
        private int? _dictionaryId;

        public bool IsLPDCollocation;
        public EnglishVariant? Language;
        public List<DicEntry> LPDEntries;
        public LDOCEHtmlEntry LDOCEEntry;
        public MWHtmlEntry MWEntry;
        public HashSet<string> AlternativeSpellings;

        public const int DictionaryIdLDOCE = 1;
        public const int DictionaryIdMW = 2;

        public string Keyword
        {
            get { return _keyword; }
        }

        public string Title
        {
            get { return _title; }
            set 
            { 
                _title = value;
                _keyword = PrepareKeyword(value);
            }
        }

        public int? DictionaryId
        {
            get { return _dictionaryId; }
        }

        public bool IsLDOCEEntry
        {
            get { return _dictionaryId == DictionaryIdLDOCE; }
            set { _dictionaryId = DictionaryIdLDOCE; }
        }

        public bool IsMWEntry
        {
            get { return _dictionaryId == DictionaryIdMW; }
            set { _dictionaryId = DictionaryIdMW; }
        }

        public static string PrepareKeyword(string keyword)
        {
            return keyword.Replace("<sub>", "").Replace("</sub>", "").Replace("&amp;", "&")
                .Replace('–', '-').Replace('‘', '\'').Replace('ʻ', '\'').Replace('“', '"').Replace('”', '"')
                .Replace("\u0327", "").Replace("\u0306", "").Replace("\u0323", "");
        }
    }

    class DicEntry
    {
        public string EntryNumber;
        public string RawMainData;

        public List<DicItem> AllItems;
    }

    enum ItemType
    {
        WordForm,
        Collocation
    }

    class ItemGroup
    {
        public ItemType GroupType;
        public List<DicItem> Items;
    }

    class DicItem
    {
        public string RawData;
        public ItemType ItemType;
    }
}
