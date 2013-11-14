using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    class SoundCollector
    {
        public string MainSoundUK { get; private set; }
        public string MainSoundUS { get; private set; }
        public List<SoundInfo> Sounds { get; private set; }

        public SoundCollector()
        {
            Sounds = new List<SoundInfo>();
        }

        public bool HasUKSound
        {
            get { return !string.IsNullOrEmpty(MainSoundUK); }
        }

        public bool HasUSSound
        {
            get { return !string.IsNullOrEmpty(MainSoundUS); }
        }

        public void RegisterSound(string soundKey, bool isUKSound)
        {
            Sounds.Add(new SoundInfo(soundKey, isUKSound));

            if (isUKSound)
            {
                if (string.IsNullOrEmpty(MainSoundUK))
                {
                    MainSoundUK = soundKey;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(MainSoundUS))
                {
                    MainSoundUS = soundKey;
                }
            }
        }
    }
}
