using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    class LDOCEEntry
    {
        public string Keyword;
        public bool IsDuplicate;
        public List<LDOCEEntryItem> Items;
    }

    class LDOCEEntryItem
    {
        public string ItemNumber;
        public string ItemKeyword;
        public string ItemTitle;
        public string AlternativeSpelling;
        public string Transcription;
        public List<string> PartsOfSpeech;
        public string Notes;
        public string SoundFileUK;
        public string SoundFileUS;
        public string RawData;

        public bool HasAudio
        {
            get { return !string.IsNullOrEmpty(SoundFileUK) || !string.IsNullOrEmpty(SoundFileUS); }
        }
    }
}
