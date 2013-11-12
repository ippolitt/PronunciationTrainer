using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Providers
{
    public class PageInfo
    {
        public bool IsWord {get; private set;}
        public string PageName { get; private set; }
        public IndexEntry Index { get; set; }

        public PageInfo(bool isWord, string pageName)
        {
            IsWord = isWord;
            PageName = pageName;
        }
    }
}
