using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    class LDOCEEntry
    {
        public string Keyword;
        public List<LDOCEEntryItem> Items;
    }

    class LDOCEEntryItem
    {
        public string ItemNumber;
        public string ItemText;
        public string ItemStressedText;
        public string Transcription;
        public string[] PartsOfSpeech;
        public string SoundFileUK;
        public string SoundFileUS;
        public string RawData;

        public bool HasAudio
        {
            get { return !string.IsNullOrEmpty(SoundFileUK) || !string.IsNullOrEmpty(SoundFileUS); }
        }
    }
}
