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
        public IndexEntryImitation(string entryText)
            : base(entryText, null, null, null, 1)
        {
        }

        public bool IsServiceItem
        {
            get { return true; }
        }
    }
}
