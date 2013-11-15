﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    class WordDescription
    {
        public string Text;
        public string SoundKeyUK;
        public string SoundKeyUS;
        public List<SoundInfo> Sounds;
        public WordUsageInfo UsageInfo;
        public List<CollocationDescription> Collocations;
    }

    class CollocationDescription
    {
        public string Text;
        public string SoundKeyUK;
        public string SoundKeyUS;
    }
}