using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    class SoundCollector
    {
        private string _mainSoundUK;
        private string _mainSoundUS;

        public bool HasMainSounds { get; private set; }
        public List<SoundInfo> Sounds { get; private set; }

        public SoundCollector()
        {
            Sounds = new List<SoundInfo>();
        }

        public string MainSoundUK
        {
            get { return HasMainSounds ? _mainSoundUK : null; }
        }

        public string MainSoundUS
        {
            get { return HasMainSounds ? _mainSoundUS : null; }
        }

        public void RegisterSound(string soundKey, bool isUKSound)
        {
            Sounds.Add(new SoundInfo(soundKey, isUKSound));

            if (isUKSound)
            {
                if (string.IsNullOrEmpty(_mainSoundUK))
                {
                    _mainSoundUK = soundKey;
                    CheckSounds();
                }
            }
            else
            {
                if (string.IsNullOrEmpty(_mainSoundUS))
                {
                    _mainSoundUS = soundKey;
                    CheckSounds();
                }
            }
        }

        private void CheckSounds()
        {
            if (!string.IsNullOrEmpty(_mainSoundUK) && !string.IsNullOrEmpty(_mainSoundUS))
            {
                HasMainSounds = true;
            }
        }
    }
}
