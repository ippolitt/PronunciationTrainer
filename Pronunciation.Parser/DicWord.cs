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

        private const int DictionaryIdLDOCE = 1;
        private const int DictionaryIdMW = 2;

        public string Keyword
        {
            get { return _keyword; }
            set 
            {
                _title = value;
                if (string.IsNullOrEmpty(_title))
                {
                    _keyword = _title;
                }
                else
                {
                    // Some keywords contain HTML tags in them (e.g. H20)
                    _keyword = _title.Replace("<sub>", "").Replace("</sub>", "");                    
                }
            }
        }

        public string Title
        {
            get { return _title; }
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
