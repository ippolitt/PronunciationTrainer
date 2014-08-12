using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Pronunciation.Core.Contexts;

namespace Pronunciation.Core.Providers.Dictionary
{
    public abstract class LPDProviderBase
    {
        protected string BaseFolder { get; private set; }

        public LPDProviderBase(string baseFolder)
        {
            BaseFolder = baseFolder;
        }

        public List<KeyTextPair<string>> GetWordLists()
        {
            return new List<KeyTextPair<string>> { 
                new KeyTextPair<string>("1000", "Top 1000 words"),
                new KeyTextPair<string>("2000", "Top 2000 words"),
                new KeyTextPair<string>("3000", "Top 3000 words"),
                new KeyTextPair<string>("5000", "Top 5000 words"),
                new KeyTextPair<string>("7500", "Top 7500 words")
            };
        }

        public PageInfo LoadListPage(string pageKey)
        {
            return new PageInfo(false, pageKey, BuildWordListPath(pageKey));
        }

        protected Uri BuildWordListPath(string listName)
        {
            return new Uri(Path.Combine(BaseFolder, string.Format(@"{0}.html", listName.ToLower())));
        }
    }
}
