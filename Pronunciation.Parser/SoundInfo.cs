using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    public class SoundInfo
    {
        public string SoundKey { get; private set; }
        public string SoundIndex { get; private set; }
        public bool IsUKSound { get; private set; }

        public SoundInfo(string soundKey, string soundIndex, bool isUKSound)
        {
            SoundKey = soundKey;
            SoundIndex = soundIndex;
            IsUKSound = isUKSound;
        }
    }
}
