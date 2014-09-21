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
        private readonly Dictionary<string, DicWord> _caseSensitiveMap;
        private readonly Dictionary<string, DicWord> _caseInsensitiveMap;
        private readonly StringBuilder _stats;

        public EntriesMapper(List<DicWord> words)
        {
            _words = words;
            _caseSensitiveMap = new Dictionary<string, DicWord>();
            _caseInsensitiveMap = new Dictionary<string, DicWord>(StringComparer.OrdinalIgnoreCase);
            foreach (var word in words)
            {
                _caseSensitiveMap.Add(word.Keyword, word);
                if (!_caseInsensitiveMap.ContainsKey(word.Keyword))
                {
                    _caseInsensitiveMap.Add(word.Keyword, word);
                }
            }
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
                MergeLDOCEEntry,
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
                MergeMWEntry,
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
                // First, trying to find case sensitive match
                DicWord word;
                if (_caseSensitiveMap.TryGetValue(entry.Keyword, out word))
                {
                    matchAction(word, entry);
                }
                else
                {
                    if (_caseInsensitiveMap.TryGetValue(entry.Keyword, out word))
                    {
                        matchAction(word, entry);
                    }
                    else
                    {
                        if (allowNew && newWordBuilder != null)
                        {
                            word = newWordBuilder(entry);
                            _words.Add(word);
                            _caseSensitiveMap.Add(entry.Keyword, word);
                            _caseInsensitiveMap.Add(entry.Keyword, word);

                            addedEntriesCount++;
                        }
                    }
                }
            }

            _stats.AppendFormat("Totally '{0}' new words have been added.\r\n", addedEntriesCount);
        }

        private void MergeLDOCEEntry(DicWord word, IExtraEntry entry)
        {
            LDOCEHtmlEntry source = (LDOCEHtmlEntry)entry;
            if (word.LDOCEEntry == null)
            {
                word.LDOCEEntry = source;
                return;
            }

            if (source.Items == null || source.Items.Count == 0)
                return;

            if (word.LDOCEEntry.Items == null || word.LDOCEEntry.Items.Count == 0)
            {
                word.LDOCEEntry.Items = source.Items;
                return;
            }

            MergeEntryItems(word.LDOCEEntry, entry, word.LDOCEEntry.Keyword, "LDOCE",
                (sourceItem) => word.LDOCEEntry.Items.Add((LDOCEHtmlEntryItem)sourceItem),
                (sourceItem, targetItems) =>
                {
                    var item = (LDOCEHtmlEntryItem)sourceItem;
                    var items = (IEnumerable<LDOCEHtmlEntryItem>)targetItems;
                    return items.FirstOrDefault(x => x.SoundFileUK == item.SoundFileUK && x.SoundFileUS == item.SoundFileUS
                        && (x.TranscriptionUK == item.TranscriptionUK || string.IsNullOrEmpty(item.TranscriptionUK)) 
                        && (x.TranscriptionUS == item.TranscriptionUS || string.IsNullOrEmpty(item.TranscriptionUS)));
                });
        }

        private void MergeMWEntry(DicWord word, IExtraEntry entry)
        {
            MWHtmlEntry source = (MWHtmlEntry)entry;
            if (word.MWEntry == null)
            {
                word.MWEntry = source;
                return;
            }

            if (source.Items == null || source.Items.Count == 0)
                return;

            if (word.MWEntry.Items == null || word.MWEntry.Items.Count == 0)
            {
                word.MWEntry.Items = source.Items;
                return;
            }

            MergeEntryItems(word.MWEntry, entry, word.MWEntry.Keyword, "MW",
                (sourceItem) => word.MWEntry.Items.Add((MWHtmlEntryItem)sourceItem),
                (sourceItem, targetItems) => 
                {
                    var item = (MWHtmlEntryItem)sourceItem;

                    // We cannot merge word forms
                    if (item.WordForms != null)
                        return null;

                    var items = (IEnumerable<MWHtmlEntryItem>)targetItems;
                    return items.FirstOrDefault(x => CollectionsEqual(x.SoundFiles, item.SoundFiles)
                        && (x.Transcription == item.Transcription || string.IsNullOrEmpty(item.Transcription)));
                });
        }

        private void MergeEntryItems(IExtraEntry target, IExtraEntry source, string keyword, string title, 
            Action<IExtraEntryItem> actionAdd, Func<IExtraEntryItem, IEnumerable<IExtraEntryItem>, IExtraEntryItem> match)
        {
            if (source.Items == null)
                return;

            if (target.Items == null)
                throw new NotSupportedException();

            bool resetNumbers = false;
            var targetItems = target.Items;
            foreach (var sourceItem in source.Items)
            {
                IExtraEntryItem targetItem = match(sourceItem, targetItems);
                if (targetItem != null)
                {
                    _stats.AppendFormat("Merged {4} item '{0} {1}' with item '{2} {3}'.\r\n",
                        sourceItem.DisplayName, sourceItem.Number, targetItem.DisplayName, targetItem.Number, title);

                    targetItem.PartsOfSpeech = MergePartsOfSpeech(targetItem.PartsOfSpeech, sourceItem.PartsOfSpeech);
                    if (targetItem.DisplayName != sourceItem.DisplayName)
                    {
                        string separator = ", ";
                        if (targetItem.DisplayName.Contains(",") || sourceItem.DisplayName.Contains(","))
                        {
                            separator = "; ";
                        }

                        targetItem.DisplayName += separator + sourceItem.DisplayName;
                    }
                }
                else
                {
                    _stats.AppendFormat("Merged {3} item '{0} {1}' with entry '{2}'.\r\n",
                        sourceItem.DisplayName, sourceItem.Number, keyword, title);

                    actionAdd(sourceItem);
                    resetNumbers = true;
                }
            }

            if (resetNumbers)
            {
                int i = 0;
                foreach (var targetItem in targetItems)
                {
                    i++;
                    targetItem.Number = i;
                }
            }
        }

        private bool CollectionsEqual(ICollection<string> target, ICollection<string> source)
        {
            if (source != null && target != null)
            {
                if (source.Count != target.Count)
                    return false;

                foreach (var item in source)
                {
                    if (!target.Contains(item))
                        return false;
                }

                return true;
            }
            else
            {
                return (source == null && target == null);
            }
        }

        private string MergePartsOfSpeech(string target, string source)
        {
            if (string.IsNullOrEmpty(target))
                return source;

            if (string.IsNullOrEmpty(source))
                return target;

            var targetParts = target.Split(',').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList();
            var sourceParts = source.Split(',').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList();

            targetParts.AddRange(sourceParts.Where(x => !targetParts.Contains(x)));
            return string.Join(", ", targetParts.Distinct().OrderBy(x => x));
        }
    }
}
