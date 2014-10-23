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

        public SoundManager(IFileLoader fileLoader, DATFileBuilder lpdBuilder, DATFileBuilder mwBuilder)
        {
            _fileLoader = fileLoader;
            _lpdBuilder = lpdBuilder;
            _mwBuilder = mwBuilder;
            _sounds = new Dictionary<string, RegisteredSound>(StringComparer.OrdinalIgnoreCase);
        }

        public static string ConvertLDOCEToLPD(string ldoceSoundKey)
        {
            if (ldoceSoundKey.StartsWith(LDOCE_OriginalPrefixUK))
                return LPD_OriginalPrefixUK + ldoceSoundKey.Remove(0, LDOCE_OriginalPrefixUK.Length);
            
            if (ldoceSoundKey.StartsWith(LDOCE_OriginalPrefixUS))
                return LPD_OriginalPrefixUS + ldoceSoundKey.Remove(0, LDOCE_OriginalPrefixUS.Length);

            throw new NotSupportedException();
        }

        public static string ConvertLPDToLDOCE(string lpdSoundKey)
        {
            if (lpdSoundKey.StartsWith(LPD_OriginalPrefixUK))
                return LDOCE_OriginalPrefixUK + lpdSoundKey.Remove(0, LPD_OriginalPrefixUK.Length);

            if (lpdSoundKey.StartsWith(LPD_OriginalPrefixUS))
                return LDOCE_OriginalPrefixUS + lpdSoundKey.Remove(0, LPD_OriginalPrefixUS.Length);

            throw new NotSupportedException();
        }

        public bool RegisterSound(SoundInfo soundInfo, out RegisteredSound registeredSound)
        {
            string soundKey = soundInfo.SoundKey;
            if (_sounds.TryGetValue(soundKey, out registeredSound))
                return false;

            bool isMWSound = soundKey.StartsWith(MW_SoundKeyPrefix);

            DataIndex soundIndex = null;
            byte[] audioData = _fileLoader.GetRawData(soundKey);
            if (audioData != null && audioData.Length > 0)
            {
                DATFileBuilder datBuilder = isMWSound ? _mwBuilder : _lpdBuilder;
                soundIndex = datBuilder.AppendEntity(soundKey, audioData);
            }

            registeredSound = new RegisteredSound 
            {
                DATFileId = isMWSound ? MWFileId : (int?)null,
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
