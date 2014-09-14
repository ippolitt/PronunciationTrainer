using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    class MWEntry
    {
        public string Keyword;
        public List<MWEntryItem> Items;
    }

    class MWEntryItem
    {
        public string ItemNumber;
        public string Transcription;
        public List<string> PartsOfSpeech;
        public List<string> SoundFiles;

        public string RawData;

        public bool HasSounds
        {
            get { return SoundFiles != null && SoundFiles.Count > 0; }
        }
    }
}
