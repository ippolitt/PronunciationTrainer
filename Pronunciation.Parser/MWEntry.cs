using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    class MWEntry
    {
        public string Keyword;
        public DisplayName Title;
        public bool IsDerived;
        public bool IsVariant;
        public List<MWEntryItem> Items;
    }

    class MWEntryItem
    {
        public DisplayName ItemTitle;
        public string ItemNumber;
        public string Transcription;
        public bool IsRawTranscription;
        public List<string> PartsOfSpeech;
        public List<string> SoundFiles;
        public List<MWWordForm> WordForms;
        public List<MWEntry> Variants;

        public string RawData;

        public bool HasSounds
        {
            get { return SoundFiles != null && SoundFiles.Count > 0; }
        }
    }

    class MWWordForm
    {
        public DisplayName Title;
        public bool IsPluralForm;
        public string Transcription;
        public List<string> SoundFiles;
    }
}
