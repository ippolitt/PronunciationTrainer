using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Providers.Dictionary
{
    public class PageInfo
    {
        public bool LoadByUrl { get; private set; }
        public Uri PageUrl { get; private set; }
        public string PageHtml { get; private set; }

        public PageInfo(Uri pageUrl) 
        {
            LoadByUrl = true;
            PageUrl = pageUrl;
        }

        public PageInfo(string pageHtml)
        {
            LoadByUrl = false;
            PageHtml = pageHtml;
        }
    }
}
