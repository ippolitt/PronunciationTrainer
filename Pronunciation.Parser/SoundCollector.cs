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

        private SoundTitleBuilder _titleBuilder;
        private string _entryNumber;

        public SoundCollector()
        {
            Sounds = new List<SoundInfo>();
        }

        public SoundCollector(SoundTitleBuilder titleBuilder, string entryNumber)
            : this()
        {
            _titleBuilder = titleBuilder;
            _entryNumber = entryNumber;
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
            var sound = new SoundInfo(soundKey, isUKSound);
            Sounds.Add(sound);

            bool isMainSound = false;
            if (isUKSound)
            {
                if (string.IsNullOrEmpty(MainSoundUK))
                {
                    MainSoundUK = soundKey;
                    isMainSound = true;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(MainSoundUS))
                {
                    MainSoundUS = soundKey;
                    isMainSound = true;
                }
            }

            if (isMainSound && _titleBuilder != null)
            {
                sound.SoundTitle = _titleBuilder.GetSoundTitle(soundKey, _entryNumber);
            }
        }

        public void SetMainSoundsTitle(string soundTitle)
        {
            if (!string.IsNullOrEmpty(MainSoundUK))
            {
                Sounds.First(x => x.SoundKey == MainSoundUK).SoundTitle = soundTitle;
            }

            if (!string.IsNullOrEmpty(MainSoundUS))
            {
                Sounds.First(x => x.SoundKey == MainSoundUS).SoundTitle = soundTitle;
            }
        }
    }
}
