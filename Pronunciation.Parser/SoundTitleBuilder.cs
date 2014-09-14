using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    class SoundTitleBuilder
    {
        private readonly string _wordText;
        private readonly bool _addNumber;
        private readonly Dictionary<string, string> _keysMap; 

        public SoundTitleBuilder(string wordText, int entriesCount)
        {
            _wordText = wordText;
            _addNumber = entriesCount > 1;
            _keysMap = new Dictionary<string, string>();
        }

        public string GetSoundTitle(string soundKey)
        {
            return GetSoundTitle(soundKey, null);
        }

        public string GetSoundTitle(string soundKey, string entryNumber)
        {
            string text;
            if (_keysMap.TryGetValue(soundKey, out text))
                return text;

            if (_addNumber && !string.IsNullOrEmpty(entryNumber))
            {
                text = string.Format("{0}, {1}", _wordText, entryNumber);
            }
            else
            {
                text = _wordText;
            }
            _keysMap.Add(soundKey, text);

            return text;
        }
    }
}
