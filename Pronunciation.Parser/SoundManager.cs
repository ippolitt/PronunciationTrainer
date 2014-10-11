using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    class SoundManager
    {
        public class RegisteredSound
        {
            public string SoundIndex;
            public int? DATFileId;
        }

        public const string LDOCE_SoundKeyPrefix = "ldoce_";
        public const string MW_SoundKeyPrefix = "mw_";

        private const string LDOCE_OriginalPrefixUK = "bre_";
        private const string LDOCE_OriginalPrefixUS = "ame_";
        private const string LPD_OriginalPrefixUK = "uk_";
        private const string LPD_OriginalPrefixUS = "us_";

        private const int MWFileId = 1;

        private readonly DATFileBuilder _lpdBuilder;
        private readonly DATFileBuilder _mwBuilder;
        private readonly IFileLoader _fileLoader;
        private readonly Dictionary<string, RegisteredSound> _sounds;

        public StringBuilder Stats { get; set; }
        public int ReusedKeysCount { get; set; }

        public SoundManager(IFileLoader fileLoader, DATFileBuilder lpdBuilder, DATFileBuilder mwBuilder)
        {
            _fileLoader = fileLoader;
            _lpdBuilder = lpdBuilder;
            _mwBuilder = mwBuilder;
            _sounds = new Dictionary<string, RegisteredSound>(StringComparer.OrdinalIgnoreCase);
        }

        public static int? GetDictionaryId(string soundKey)
        {
            if (soundKey.StartsWith(LDOCE_SoundKeyPrefix))
            {
                return DicWord.DictionaryIdLDOCE;
            }
            else if (soundKey.StartsWith(MW_SoundKeyPrefix))
            {
                return DicWord.DictionaryIdMW;
            }
            else
            {
                return null;
            }
        }

        public bool RegisterSound(SoundInfo soundInfo, out RegisteredSound registeredSound)
        {
            string soundKey = soundInfo.SoundKey;
            if (_sounds.TryGetValue(soundKey, out registeredSound))
                return false;

            string alternativeKey = null;
            DATFileBuilder datBuilder;
            if (soundKey.StartsWith(LDOCE_SoundKeyPrefix))
            {
                datBuilder = _lpdBuilder;

                // If we find corresponding LPD (e.g. bre_test -> uk_test) we consider that audio is the same
                string originalKey = soundKey.Remove(0, LDOCE_SoundKeyPrefix.Length);
                if (originalKey.StartsWith(LDOCE_OriginalPrefixUK))
                {
                    alternativeKey = LPD_OriginalPrefixUK + originalKey.Remove(0, LDOCE_OriginalPrefixUK.Length);
                }
                else if (originalKey.StartsWith(LDOCE_OriginalPrefixUS))
                {
                    alternativeKey = LPD_OriginalPrefixUS + originalKey.Remove(0, LDOCE_OriginalPrefixUS.Length);
                }
            }
            else if (soundKey.StartsWith(MW_SoundKeyPrefix))
            {
                datBuilder = _mwBuilder;
            }
            else
            {
                datBuilder = _lpdBuilder;

                // If we find corresponding LDOCE (e.g. uk_test -> bre_test) we consider that audio is the same
                if (soundKey.StartsWith(LPD_OriginalPrefixUK))
                {
                    alternativeKey = LDOCE_SoundKeyPrefix + LDOCE_OriginalPrefixUK + soundKey.Remove(0, LPD_OriginalPrefixUK.Length);
                }
                else if (soundKey.StartsWith(LPD_OriginalPrefixUS))
                {
                    alternativeKey = LDOCE_SoundKeyPrefix + LDOCE_OriginalPrefixUS + soundKey.Remove(0, LPD_OriginalPrefixUS.Length);
                }
            }

            if (!string.IsNullOrEmpty(alternativeKey))
            {
                if (_sounds.TryGetValue(alternativeKey, out registeredSound))
                {
                    _sounds.Add(soundKey, registeredSound);
                    ReusedKeysCount++;

                    return true;
                }
            }

            if (datBuilder == null)
                throw new ArgumentNullException("DAT file builder is not initialized!");

            byte[] audioData = _fileLoader.GetRawData(soundKey);
            DataIndex soundIndex = null;
            if (audioData != null && audioData.Length > 0)
            {
                soundIndex = datBuilder.AppendEntity(soundKey, audioData);
            }

            registeredSound = new RegisteredSound 
            {
                DATFileId = ReferenceEquals(datBuilder, _mwBuilder) ? MWFileId : (int?)null,
                SoundIndex = soundIndex == null ? null : soundIndex.BuildKey() 
            };

            _sounds.Add(soundKey, registeredSound);
            return true;
        }

        public void Flush()
        {
            if (_lpdBuilder != null)
            {
                _lpdBuilder.Flush();
            }
            if (_mwBuilder != null)
            {
                _mwBuilder.Flush();
            }
        }
    }
}
