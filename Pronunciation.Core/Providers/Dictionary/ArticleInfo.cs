using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Providers.Dictionary
{
    public class ArticlePage : PageInfo
    {
        public string ArticleKey { get; private set; }

        public ArticlePage(string articleKey, Uri pageUrl) 
            : base(pageUrl)
        {
            ArticleKey = articleKey;
        }

        public ArticlePage(string articleKey, string pageHtml)
            : base(pageHtml)
        {
            ArticleKey = articleKey;
        }

        public override bool Equals(object obj)
        {
            return AreEqual(this, (ArticlePage)obj);
        }

        public static bool operator ==(ArticlePage a, ArticlePage b)
        {
            return AreEqual(a, b);
        }

        public static bool operator !=(ArticlePage a, ArticlePage b)
        {
            return !AreEqual(a, b);
        }

        private static bool AreEqual(ArticlePage a, ArticlePage b)
        {
            if (ReferenceEquals(a, null))
                return ReferenceEquals(b, null);

            if (ReferenceEquals(b, null))
                return false;

            return string.Equals(a.ArticleKey, b.ArticleKey);
        }

        public override int GetHashCode()
        {
            return string.IsNullOrEmpty(ArticleKey) ? 0 : ArticleKey.GetHashCode();
        }
    }
}
