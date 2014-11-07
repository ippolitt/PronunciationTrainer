using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    class WordListNode
    {
        public string PageName;
        public string Keyword;
        public WordListNode PreviousWord;
        public WordListNode NextWord;
        public int Rank;
    }
}
