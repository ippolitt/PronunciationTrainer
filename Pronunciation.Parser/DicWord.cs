using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    class DicWord
    {
        public string Keyword;
        public List<DicEntry> LPDEntries;
        public LDOCEHtmlEntry LDOCEEntry;
        public bool IsLDOCEEntry;
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
