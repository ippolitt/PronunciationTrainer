﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    public class LDOCEHtmlEntry : IExtraEntry
    {
        public string Keyword { get; set; }
        public List<LDOCEHtmlEntity> Items { get; set; }
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
