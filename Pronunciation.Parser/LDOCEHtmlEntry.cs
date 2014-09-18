using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace Pronunciation.Parser
{
    public class LDOCEHtmlEntry : IExtraEntry
    {
        public string Keyword { get; set; }
        public List<LDOCEHtmlEntryItem> Items { get; set; }

        IEnumerable<IExtraEntryItem> IExtraEntry.Items
        {
            get { return (IEnumerable<IExtraEntryItem>)Items; }
        }
    }

    public class LDOCEHtmlEntryItem : IExtraEntryItem
    {
        public int Number { get; set; }
        public string DisplayName { get; set; }
        public string TranscriptionUK { get; set; }
        public string TranscriptionUS { get; set; }
        public string PartsOfSpeech { get; set; }
        public string SoundFileUK { get; set; }
        public string SoundFileUS { get; set; }
        public string Notes { get; set; }
    }
}
