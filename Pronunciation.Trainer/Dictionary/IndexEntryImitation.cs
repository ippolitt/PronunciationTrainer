using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Providers.Dictionary;
using Pronunciation.Trainer.Controls;

namespace Pronunciation.Trainer.Dictionary
{
    public class IndexEntryImitation : IndexEntry, ISuggestionItemInfo
    {
        private const string ImmitationEntryKey = "@";

        public IndexEntryImitation(string entryText)
            : base(ImmitationEntryKey, entryText, false, null, null, null, false, null)
        {
        }

        public bool IsServiceItem
        {
            get { return true; }
        }
    }
}
