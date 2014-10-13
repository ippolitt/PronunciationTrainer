using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Providers.Dictionary;

namespace Pronunciation.Trainer.Dictionary
{
    public class DictionaryIndex
    {
        private IndexEntry[] _entries;
        private TokenizedIndexEntry[] _tokens;
        private bool _isInitialized;

        public void Build(IEnumerable<IndexEntry> entries)
        {
            _isInitialized = false;
            if (entries == null)
                return;

            // Build index for StartsWith match
            _entries = entries.OrderBy(x => x.DisplayName).ToArray();

            // Build index for token-based match (split a phrase into words, each word can be searched using StartsWith)
            _tokens = BuildTokensIndex(_entries);

            _isInitialized = true;
        }

        public int EntriesCount
        {
            get { return _entries.Length; }
        }

        public IEnumerable<IndexEntry> Entries
        {
            get { return _entries; }
        }

        public IndexEntry GetWordByName(string wordName)
        {
            if (!_isInitialized)
                return null;

            return _entries.FirstOrDefault(x => x.DisplayName == wordName);
        }

        public IndexEntry GetWordById(int wordId)
        {
            if (!_isInitialized)
                return null;

            return _entries.Single(x => x.WordId == wordId);
        }

        public IndexEntry GetEntryByPosition(int entryPosition)
        {
            if (!_isInitialized || entryPosition < 0)
                return null;

            return entryPosition < _entries.Length ? _entries[entryPosition] : null;
        }

        public int GetEntryPosition(IndexEntry entry)
        {
            if (!_isInitialized)
                return -1;

            for (int i = 0; i < _entries.Length; i++)
            {
                if (ReferenceEquals(_entries[i], entry))
                    return i;
            }

            return -1;
        }

        public List<IndexEntry> FindEntriesByText(string searchText, bool isExactMatch, int maxItems)
        {
            if (!_isInitialized || string.IsNullOrWhiteSpace(searchText))
                return null;

            // Add main matches
            searchText = searchText.Trim();
            IEnumerable<IndexEntry> mainQuery;
            if (isExactMatch)
            {
                mainQuery = _entries.Where(x => string.Equals(x.DisplayName, searchText, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                mainQuery = _entries.Where(x => x.DisplayName.StartsWith(searchText, StringComparison.OrdinalIgnoreCase));
            }
            if (maxItems >= 0)
            {
                mainQuery = mainQuery.Take(maxItems);
            }
            var comparer = new IndexEntryComparer(searchText);
            var entries = mainQuery.OrderBy(x => x, comparer).ToList();

            // Add token based matches
            if (!isExactMatch && (entries.Count < maxItems || maxItems < 0))
            {
                var tokensQuery = _tokens.Where(x => x.Token.StartsWith(searchText, StringComparison.OrdinalIgnoreCase));
                if (maxItems >= 0)
                {
                    tokensQuery = tokensQuery.Take(maxItems - entries.Count);
                }

                var tokenEntries = tokensQuery.OrderBy(x => x.Rank).ThenBy(x => x.Entry.DisplayName).Select(x => x.Entry).ToList();
                if (tokenEntries.Count > 0)
                {
                    // Add only those entries that don't already present in the list
                    var hashSet = new HashSet<IndexEntry>(entries);
                    entries.AddRange(tokenEntries.Where(x => hashSet.Add(x)));
                }
            }

            return entries;
        }

        private static TokenizedIndexEntry[] BuildTokensIndex(IndexEntry[] entries)
        {
            var tokens = new List<TokenizedIndexEntry>();
            foreach (var entry in entries)
            {
                int rank = 0;
                int position = 0;
                bool isTokenStart = false;
                foreach (char ch in entry.DisplayName)
                {
                    if (ch == ' ' || ch == '-' || ch == ',' || ch == '/' || ch == '(' || ch == ')')
                    {
                        // As soon as we hit a separator the next character might be the beginning of a token
                        // It means that we'll skip first words (because this case is covered by the _wordsIndex array)
                        isTokenStart = true;
                    }
                    else
                    {
                        if (isTokenStart)
                        {
                            isTokenStart = false;

                            // Add the whole remaining string as a token to enable multi-words match: 
                            // e.g. match "lot car" in "parking lot car"
                            string token = entry.DisplayName.Substring(position);

                            // Don't add 1-symbol tokens
                            if (token.Length > 1)
                            {
                                tokens.Add(new TokenizedIndexEntry
                                {
                                    Rank = rank,
                                    Token = token,
                                    Entry = entry
                                });
                            }

                            rank++;
                        }
                    }

                    position++;
                }
            }

            return tokens.OrderBy(x => x.Rank).ThenBy(x => x.Entry.DisplayName).ToArray();
        }

        private class TokenizedIndexEntry
        {
            public string Token;
            public int Rank;
            public IndexEntry Entry;
        }

        private class IndexEntryComparer : IComparer<IndexEntry>
        {
            private readonly string _searchText;

            public IndexEntryComparer(string searchText)
            {
                _searchText = searchText;
            }

            public int Compare(IndexEntry x, IndexEntry y)
            {
                int result;
                if (x != null && y != null)
                {
                    result = string.Compare(x.DisplayName, y.DisplayName);
                    if (result != 0)
                    {
                        // Check if they differ only by case and ensure that case-sensitive match with the search text 
                        // is higher than case-insensitive one. So if search text is "A", then we display: "A, a" 
                        if (string.Equals(x.DisplayName, y.DisplayName, StringComparison.OrdinalIgnoreCase))
                        {
                            if (x.DisplayName.StartsWith(_searchText))
                            {
                                result = -1;
                            }
                            else if (y.DisplayName.StartsWith(_searchText))
                            {
                                result = 1;
                            }
                        }
                    }
                }
                else
                {
                    result = (x == null && y == null) ? 0 : (x == null ? -1 : 1);
                }

                return result;
            }
        }
    }
}
