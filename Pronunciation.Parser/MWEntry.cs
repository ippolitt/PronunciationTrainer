using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    class MWEntry
    {
        public string Keyword;
        public string Title;
        public List<MWEntryItem> Items;
    }

    class MWEntryItem
    {
        public string ItemTitle;
        public string ItemNumber;
        public string Transcription;
        public List<string> PartsOfSpeech;
        public List<string> SoundFiles;
        public List<MWWordForm> WordForms;

        public string RawData;

        public bool HasSounds
        {
            get { return SoundFiles != null && SoundFiles.Count > 0; }
        }
    }

    class MWWordForm
    {
        public string FormName;
        public bool IsPluralForm;
        public string Transcription;
        public List<string> SoundFiles;
    }
}
