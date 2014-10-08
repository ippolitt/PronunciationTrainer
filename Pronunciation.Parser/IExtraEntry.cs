using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    interface IExtraEntry
    {
        string Keyword { get; }
        IEnumerable<IExtraEntryItem> Items { get; }
    }

    interface IExtraEntryItem
    {
        int Number { get; set; }
        DisplayName Title { get; set; }
        string PartsOfSpeech { get; set; }
    }
}
