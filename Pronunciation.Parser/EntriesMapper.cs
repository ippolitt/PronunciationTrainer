using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pronunciation.Parser
{
    class EntriesMapper
    {
        private readonly List<DicWord> _words;
        private readonly Dictionary<string, DicWord> _wordsMap;
        private readonly HashSet<string> _collocations;
        private readonly StringBuilder _stats;

        public EntriesMapper(List<DicWord> words, IEnumerable<string> collocations)
        {
            _words = words;
            _collocations = new HashSet<string>(collocations);
            _wordsMap = _words.ToDictionary(x => x.Keyword);
            _stats = new StringBuilder();
        }

        public StringBuilder Stats
        {
            get { return _stats; }
        }

        public void MatchEntries(LDOCEHtmlBuilder ldoce)
        {
            if (ldoce == null)
                return;

            _stats.AppendLine("\r\nMatching LDOCE entries...");
            MatchEntries(ldoce.GetEntries(),
                (word, entry) => word.LDOCEEntry = (LDOCEHtmlEntry)entry,
                (entry) => new DicWord 
                    { 
                        Keyword = entry.Keyword, 
                        LDOCEEntry = (LDOCEHtmlEntry)entry, 
                        IsLDOCEEntry = true
                    }); 
        }

        public void MatchEntries(MWHtmlBuilder mw)
        {
            if (mw == null)
                return;
            
            _stats.AppendLine("\r\nMatching Merriam-Webster entries...");
            MatchEntries(mw.GetEntries(),
                (word, entry) => word.MWEntry = (MWHtmlEntry)entry,
                (entry) => new DicWord
                {
                    Keyword = entry.Keyword,
                    MWEntry = (MWHtmlEntry)entry,
                    IsMWEntry = true
                }); 
        }

        private void MatchEntries(IEnumerable<IExtraEntry> entries, Action<DicWord, IExtraEntry> matchAction, 
            Func<IExtraEntry, DicWord> newWordBuilder)
        {
            int collocationMatchCount = 0;
            int addedEntriesCount = 0;
            foreach(var entry in entries)
            {
                DicWord word;
                if (!_wordsMap.TryGetValue(entry.Keyword, out word))
                {
                    if (newWordBuilder != null)
                    {
                        word = newWordBuilder(entry);
                        _words.Add(word);
                        _wordsMap.Add(entry.Keyword, word);

                        addedEntriesCount++;
                    }
                }
                else
                {
                    matchAction(word, entry);
                }

                if (_collocations.Contains(entry.Keyword))
                {
                    collocationMatchCount++;
                    _stats.AppendFormat("Keyword '{0}' matches collocation.\r\n", entry.Keyword);
                }
            }

            _stats.AppendFormat("Totally '{0}' keywords match collocations.\r\n", collocationMatchCount);
            _stats.AppendFormat("Totally '{0}' new words have been added.\r\n", addedEntriesCount);
        }
    }
}
