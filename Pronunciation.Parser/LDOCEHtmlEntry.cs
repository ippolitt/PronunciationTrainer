using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    public class LDOCEHtmlEntry
    {
        public string Keyword;
        public List<LDOCEHtmlEntity> Items;
    }

    public class LDOCEHtmlEntity
    {
        public int Number;
        public string DisplayName;
        public string TranscriptionUK;
        public string TranscriptionUS;
        public string PartsOfSpeech;
        public string SoundFileUK;
        public string SoundFileUS;
    }
}
