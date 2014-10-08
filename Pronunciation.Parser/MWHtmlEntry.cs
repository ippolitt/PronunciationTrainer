using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    public class MWHtmlEntry : IExtraEntry
    {
        public string Keyword { get; set; }
        public List<MWHtmlEntryItem> Items { get; set; }
        public List<MWHtmlWordForm> WordForms { get; set; }

        IEnumerable<IExtraEntryItem> IExtraEntry.Items
        {
            get { return (IEnumerable<IExtraEntryItem>)Items; }
        }
    }

    public class MWHtmlEntryItem : IExtraEntryItem
    {
        public int Number { get; set; }
        public DisplayName Title { get; set; }
        public string Transcription { get; set; }
        public string PartsOfSpeech { get; set; }
        public string[] SoundFiles { get; set; }
    }

    public class MWHtmlWordForm
    {
        public DisplayName Title;
        public string Note;
        public string Transcription;
        public string[] SoundFiles;
    }
}
