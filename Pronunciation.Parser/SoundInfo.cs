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

        public SoundInfo(string soundKey, bool isUKSound)
        {
            SoundKey = soundKey;
            IsUKSound = isUKSound;
        }
    }
}
