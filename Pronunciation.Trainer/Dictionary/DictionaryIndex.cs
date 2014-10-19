using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Providers.Dictionary;

namespace Pronunciation.Trainer.Dictionary
{
    public class DictionaryIndex
    {
        private IndexEntry[] _mainEntries;
        private IndexEntry[] _alternativeEntries;
        private TokenizedIndexEntry[] _tokens;
        private bool _isInitialized;

        private readonly static Dictionary<char, string> ReplacementMap;

        static DictionaryIndex()
        {
            ReplacementMap = InitReplacementMap();
        }

        public void Build(IEnumerable<IndexEntry> indexEntries, bool isMainIndex)
        {
            _isInitialized = false;

            _mainEntries = null;
            _alternativeEntries = null;
            _tokens = null;
            if (indexEntries == null)
                return;
            
            // Avoid using temporary list if possible (because its size is pretty big)
            List<IndexEntry> mainList = null;
            IndexEntry[] mainArray = null;
            if (indexEntries is ICollection<IndexEntry>)
            {
                mainArray = new IndexEntry[((ICollection<IndexEntry>)indexEntries).Count];
            }
            else
            {
                mainList = new List<IndexEntry>();
            }

            var alternativeEntries = new List<IndexEntry>();
            var tokens = new List<TokenizedIndexEntry>();
            int i = 0;
            foreach (var entry in indexEntries.OrderBy(x => x.DisplayName))
            {
                // Index for StartsWith match
                if (mainList == null)
                {
                    mainArray[i] = entry;
                }
                else
                {
                    mainList.Add(entry);
                }

                // Index for token-based match (split a phrase into words, each word can be searched using StartsWith)
                AddTokens(entry.DisplayName, entry, tokens);

                // Generate alternative name (without accented characters etc.) if required
                if (isMainIndex)
                {
                    entry.AlternativeName = PrepareAlternativeName(entry.DisplayName);
                }

                if (!string.IsNullOrEmpty(entry.AlternativeName))
                {
                    alternativeEntries.Add(entry);
                    AddTokens(entry.AlternativeName, entry, tokens);
                }

                i++;
            }

            if (mainList == null)
            {
                _mainEntries = mainArray;
            }
            else
            {
                _mainEntries = mainList.ToArray();
            }
            _alternativeEntries = alternativeEntries.OrderBy(x => x.DisplayName).ToArray();
            _tokens = tokens.OrderBy(x => x.Rank).ThenBy(x => x.Entry.DisplayName).ToArray();

            var bld = new StringBuilder();
            foreach (var entry in _alternativeEntries)
            {
                bld.AppendFormat("{0}\t{1}\r\n", entry.DisplayName, entry.AlternativeName);
            }
            System.IO.File.WriteAllText(@"D:\test.txt", bld.ToString());

            _isInitialized = true;
        }

        public int EntriesCount
        {
            get { return _mainEntries.Length; }
        }

        public IEnumerable<IndexEntry> Entries
        {
            get { return _mainEntries; }
        }

        public IndexEntry GetWordByName(string wordName)
        {
            if (!_isInitialized)
                return null;

            return _mainEntries.FirstOrDefault(x => x.DisplayName == wordName);
        }

        public IndexEntry GetWordById(int wordId)
        {
            if (!_isInitialized)
                return null;

            return _mainEntries.Single(x => x.WordId == wordId);
        }

        public IndexEntry GetEntryByPosition(int entryPosition)
        {
            if (!_isInitialized || entryPosition < 0)
                return null;

            return entryPosition < _mainEntries.Length ? _mainEntries[entryPosition] : null;
        }

        public int GetEntryPosition(IndexEntry entry)
        {
            if (!_isInitialized)
                return -1;

            for (int i = 0; i < _mainEntries.Length; i++)
            {
                if (ReferenceEquals(_mainEntries[i], entry))
                    return i;
            }

            return -1;
        }

        public List<IndexEntry> FindEntriesByText(string searchText, bool isExactMatch, int maxItems)
        {
            if (!_isInitialized || string.IsNullOrWhiteSpace(searchText))
                return null;

            // Add main matches
            bool limitMaxItems = maxItems >= 0;
            searchText = searchText.Trim();
            IEnumerable<IndexEntry> mainQuery;
            if (isExactMatch)
            {
                mainQuery = _mainEntries.Where(x => string.Equals(x.DisplayName, searchText, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                mainQuery = _mainEntries.Where(x => x.DisplayName.StartsWith(searchText, StringComparison.OrdinalIgnoreCase));
            }
            if (limitMaxItems)
            {
                mainQuery = mainQuery.Take(maxItems);
            }
            var comparer = new IndexEntryComparer(searchText);
            var entries = mainQuery.OrderBy(x => x, comparer).ToList();

            if (!isExactMatch)
            {
                HashSet<IndexEntry> entriesHash = null;

                // Add alternative text matches
                int itemsLeft = maxItems - entries.Count;
                if (itemsLeft > 0 || !limitMaxItems)
                {
                    var alternativeQuery = _alternativeEntries
                        .Where(x => x.AlternativeName.StartsWith(searchText, StringComparison.OrdinalIgnoreCase));
                    if (limitMaxItems)
                    {
                        alternativeQuery = alternativeQuery.Take(itemsLeft);
                    }

                    var alternativeEntries = alternativeQuery.OrderBy(x => x.DisplayName).ToList();
                    if (alternativeEntries.Count > 0)
                    {
                        // Add only those entries that don't already present in the list
                        if (entriesHash == null)
                        {
                            entriesHash = new HashSet<IndexEntry>(entries);
                        }
                        entries.AddRange(alternativeEntries.Where(x => entriesHash.Add(x)));
                    }
                }

                // Add token based matches
                itemsLeft = maxItems - entries.Count;
                if (itemsLeft > 0 || !limitMaxItems)
                {
                    var tokensQuery = _tokens.Where(x => x.Token.StartsWith(searchText, StringComparison.OrdinalIgnoreCase));
                    if (limitMaxItems)
                    {
                        tokensQuery = tokensQuery.Take(itemsLeft);
                    }

                    var tokenEntries = tokensQuery.OrderBy(x => x.Rank).ThenBy(x => x.Entry.DisplayName).Select(x => x.Entry).ToList();
                    if (tokenEntries.Count > 0)
                    {
                        // Add only those entries that don't already present in the list
                        if (entriesHash == null)
                        {
                            entriesHash = new HashSet<IndexEntry>(entries);
                        }
                        entries.AddRange(tokenEntries.Where(x => entriesHash.Add(x)));
                    }
                }
            }

            return entries;
        }

        private static void AddTokens(string text, IndexEntry entry, List<TokenizedIndexEntry> tokens)
        {
            int rank = 0;
            int position = 0;
            int tokenStartPosition = 0;
            bool isTokenStart = false;
            foreach (char ch in text)
            {
                if (ch == ' ' || ch == '-' || ch == ',' || ch == '/' || ch == '(' || ch == '&')
                {
                    // As soon as we hit a separator the next character might be the beginning of a token
                    // It means that we'll skip first words (because this case is covered by the _wordsIndex array)
                    isTokenStart = true;
                    if (ch != ' ' && position > 0 && tokenStartPosition == 0)
                    {
                        tokenStartPosition = position;
                    }
                }
                else
                {
                    if (isTokenStart)
                    {
                        isTokenStart = false;

                        // Add the whole remaining string as a token to enable multi-words match: 
                        // e.g. match "lot car" in "parking lot car"
                        string token = text.Substring(position);

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

                        // Add also string including a separator in case a user will search like "-hand" or "/DC"
                        if (tokenStartPosition > 0)
                        {
                            token = text.Substring(tokenStartPosition);
                            tokens.Add(new TokenizedIndexEntry
                            {
                                Rank = rank,
                                Token = token,
                                Entry = entry
                            });
                        }
                        tokenStartPosition = 0;

                        rank++;
                    }
                }

                position++;
            }
        }

        private static string PrepareAlternativeName(string displayName)
        {
            StringBuilder bld = null;
            int wordEndReplacements = 0;
            int totalReplacements = 0;
            for (int i = 0; i < displayName.Length; i++)
            {
                char ch = displayName[i];

                // If word differs only by first hyphen we don't create an alternative entry (this case will be covered by tokens).
                if (ch == '-' && i == 0)
                    continue;

                string replacement;
                if (ReplacementMap.TryGetValue(ch, out replacement))
                {
                    if (bld == null)
                    {
                        // Initialize string builder with the preceding part of the string
                        bld = new StringBuilder(displayName.Substring(0, i));
                    }

                    // Don't add consecutive whitespaces
                    if (!(replacement == " " && bld.Length > 0 && bld[bld.Length - 1] == ' '))
                    {
                        bld.Append(replacement);
                    }

                    totalReplacements++;
                    wordEndReplacements++;
                }
                else
                {
                    if (bld != null)
                    {
                        // Don't add consecutive whitespaces
                        if (!(ch == ' ' && bld.Length > 0 && bld[bld.Length - 1] == ' '))
                        {
                            bld.Append(ch);
                        }
                        wordEndReplacements = 0;
                    }
                }
            }

            // If all replacements took place at the end of the string then don't generate alternative text
            if (totalReplacements > 0 && totalReplacements == wordEndReplacements)
                return null;

            return bld == null ? null : bld.ToString().Trim();
        }

        private static Dictionary<char, string> InitReplacementMap()
        {
            return new Dictionary<char, string>
            {
                // Remove
                {'"', ""},
                {',', ""},
                {'(', ""},
                {')', ""},
                {'!', ""},
                {'?', ""},
                {'¿', ""},
                {'$', ""},

                // Symbols
                {'-', " "},
                {'.', " "},
                {'\'', " "},
                {'′', " "},
                {'&', " "},
                {'*', " "},
                {'+', " "},
                {'/', " "},
                {':', " "},
                
                // Accented characters
                {'Á', "A"},
                {'Å', "A"},
                {'Æ', "Ae"},
                {'Ç', "C"},
                {'É', "E"},
                {'Í', "I"},
                {'Î', "I"},
                {'Ó', "O"},
                {'Õ', "O"},
                {'Ö', "O"},
                {'Ø', "O"},
                {'Ü', "U"},
                {'à', "a"},
                {'á', "a"},
                {'â', "a"},
                {'ã', "a"},
                {'ä', "a"},
                {'å', "a"},
                {'æ', "ae"},
                {'ç', "c"},
                {'è', "e"},
                {'é', "e"},
                {'ê', "e"},
                {'ë', "e"},
                {'ì', "i"},
                {'í', "i"},
                {'î', "i"},
                {'ï', "i"},
                {'ñ', "n"},
                {'ò', "o"},
                {'ó', "o"},
                {'ô', "o"},
                {'õ', "o"},
                {'ö', "o"},
                {'ø', "o"},
                {'ù', "u"},
                {'ú', "u"},
                {'û', "u"},
                {'ü', "u"},
                {'ý', "y"},
                {'ÿ', "y"},
                {'Ā', "A"},
                {'ā', "a"},
                {'ă', "a"},
                {'ą', "a"},
                {'ć', "c"},
                {'Č', "C"},
                {'č', "c"},
                {'ę', "e"},
                {'ě', "e"},
                {'ī', "i"},
                {'İ', "I"},
                {'ı', "i"},
                {'ł', "l"},
                {'ń', "n"},
                {'ň', "n"},
                {'ō', "o"},
                {'ő', "o"},
                {'œ', "oe"},
                {'ř', "r"},
                {'Ś', "S"},
                {'ś', "s"},
                {'ş', "s"},
                {'Š', "S"},
                {'š', "s"},
                {'ţ', "t"},
                {'ũ', "u"},
                {'ū', "u"},
                {'ŭ', "u"},
                {'ů', "u"},
                {'Ž', "Z"},
                {'ž', "z"}
            };
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
