using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    class PartOfSpeechResolver
    {
        private readonly string[] _partsOfSpeech;

        public PartOfSpeechResolver(IEnumerable<string> partsOfSpeech)
        {
            // Order is important!
            _partsOfSpeech = partsOfSpeech.Select(x => x.ToLower()).ToArray();
        }

        public string FindPartOfSpeech(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            string searchedText = text.Trim().ToLower();
            return _partsOfSpeech.FirstOrDefault(x => searchedText == x 
                || searchedText.StartsWith(x + " ") || searchedText.EndsWith(" " + x) 
                || searchedText.Contains(" " + x + " "));
        }
    }
}
