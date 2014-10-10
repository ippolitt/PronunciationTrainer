﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Database
{
    public partial class DictionaryWord
    {
        public DictionaryWord()
        {
            this.DictionaryCategories = new HashSet<DictionaryCategory>();
        }

        public string Keyword { get; set; }
        public int WordId { get; set; }
        public Nullable<int> UsageRank { get; set; }
        public string RankLongmanS { get; set; }
        public string RankLongmanW { get; set; }
        public Nullable<int> RankMacmillan { get; set; }
        public Nullable<int> RankCOCA { get; set; }
        public Nullable<int> DictionaryId { get; set; }
        public string SoundKeyUK { get; set; }
        public string SoundKeyUS { get; set; }
        public string FavoriteSoundKey { get; set; }
        public string HtmlIndex { get; set; }
        public Nullable<bool> IsCollocation { get; set; }

        public virtual ICollection<DictionaryCategory> DictionaryCategories { get; set; }
    }
}
