using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    public class SoundInfo
    {
        public string SoundKey { get; private set; }
        public bool IsUKSound { get; private set; }
        public string SoundTitle { get; set; }

        public SoundInfo(string soundKey, bool isUKSound)
            : this (soundKey, null, isUKSound)
        {
        }

        public SoundInfo(string soundKey, string soundText, bool isUKSound)
        {
            SoundKey = soundKey;
            SoundTitle = soundText;
            IsUKSound = isUKSound;
        }
    }
}
