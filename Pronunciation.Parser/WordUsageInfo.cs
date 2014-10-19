using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    class WordUsageInfo
    {
        public string Keyword;
        public int CombinedRank;
        public WordRanks Ranks;
        public WordUsageInfo PreviousWord;
        public WordUsageInfo NextWord;
        public bool IsMapped;
    }
}
