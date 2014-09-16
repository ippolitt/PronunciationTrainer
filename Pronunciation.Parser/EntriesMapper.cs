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
        private readonly StringBuilder _stats;

        public EntriesMapper(List<DicWord> words)
        {
            _words = words;
            _wordsMap = _words.ToDictionary(x => x.Keyword);
            _stats = new StringBuilder();
        }

        public StringBuilder Stats
        {
            get { return _stats; }
        }

        public void AddEntries(LDOCEHtmlBuilder ldoce, bool allowNew)
        {
            if (ldoce == null)
                return;

            _stats.AppendLine("\r\nMatching LDOCE entries...");
            AddEntries(ldoce.GetEntries(),
                (word, entry) => word.LDOCEEntry = (LDOCEHtmlEntry)entry,
                (entry) => new DicWord 
                    { 
                        Keyword = entry.Keyword, 
                        LDOCEEntry = (LDOCEHtmlEntry)entry, 
                        IsLDOCEEntry = true
                    },
                allowNew); 
        }

        public void AddEntries(MWHtmlBuilder mw, bool allowNew)
        {
            if (mw == null)
                return;
            
            _stats.AppendLine("\r\nMatching Merriam-Webster entries...");
            AddEntries(mw.GetEntries(),
                (word, entry) => word.MWEntry = (MWHtmlEntry)entry,
                (entry) => new DicWord
                {
                    Keyword = entry.Keyword,
                    MWEntry = (MWHtmlEntry)entry,
                    IsMWEntry = true
                },
                allowNew); 
        }

        private void AddEntries(IEnumerable<IExtraEntry> entries, Action<DicWord, IExtraEntry> matchAction,
            Func<IExtraEntry, DicWord> newWordBuilder, bool allowNew)
        {
            int addedEntriesCount = 0;
            foreach(var entry in entries)
            {
                DicWord word;
                if (!_wordsMap.TryGetValue(entry.Keyword, out word))
                {
                    if (allowNew && newWordBuilder != null)
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
            }

            _stats.AppendFormat("Totally '{0}' new words have been added.\r\n", addedEntriesCount);
        }
    }
}
