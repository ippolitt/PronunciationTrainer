using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    public class MWHtmlEntry : IExtraEntry
    {
        public string Keyword { get; set; }
        public List<MWHtmlEntryEntity> Items { get; set; }
    }

    public class MWHtmlEntryEntity
    {
        public int Number;
        public string DisplayName;
        public string Transcription;
        public string PartsOfSpeech;
        public string[] SoundFiles;
    }
}
