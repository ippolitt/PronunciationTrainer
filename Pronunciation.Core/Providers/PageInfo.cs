using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Providers
{
    public class PageInfo
    {
        public string PageKey { get; private set; }
        public bool IsArticle {get; private set; }
        public bool LoadByUrl { get; private set; }
        public Uri PageUrl { get; private set; }
        public string PageHtml { get; private set; }

        public IndexEntry Index { get; set; }

        private PageInfo(bool isArticle, string pageKey)
        {
            IsArticle = isArticle;
            PageKey = pageKey;
        }

        public PageInfo(bool isArticle, string pageKey, Uri pageUrl) 
            : this(isArticle, pageKey)
        {
            LoadByUrl = true;
            PageUrl = pageUrl;
        }

        public PageInfo(bool isArticle, string pageKey, string pageHtml)
            : this(isArticle, pageKey)
        {
            LoadByUrl = false;
            PageHtml = pageHtml;
        }

        public override bool Equals(object obj)
        {
            return AreEqual(this, (PageInfo)obj);
        }

        public static bool operator ==(PageInfo a, PageInfo b)
        {
            return AreEqual(a, b);
        }

        public static bool operator !=(PageInfo a, PageInfo b)
        {
            return !AreEqual(a, b);
        }

        private static bool AreEqual(PageInfo a, PageInfo b)
        {
            if (ReferenceEquals(a, null))
                return ReferenceEquals(b, null);

            if (ReferenceEquals(b, null))
                return false;
            
            return (a.IsArticle == b.IsArticle) && string.Equals(a.PageKey, b.PageKey);
        }
    }
}
