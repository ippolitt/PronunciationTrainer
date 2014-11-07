using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pronunciation.Parser
{
    class WordUsageBuilder
    {
        private Dictionary<string, WordUsageInfo> _words;
        private Dictionary<string, WordListNode> _nodes;
        private Dictionary<int, List<WordListNode>> _nodeLists;
        private string _usageFile;
        private StringBuilder _stats;

        private static string[] CanRemoveDots = new string[] { "etc.", "O.K.", "vs.", "i.e.", "Mr.", "Ms.", "Mrs.", "Dr.", "A.D.", "A.M.", "P.M." };

        public WordUsageBuilder(string usageFile)
        {
            if (string.IsNullOrEmpty(usageFile))
                throw new ArgumentNullException();

            _usageFile = usageFile;
        }

        public StringBuilder Stats
        {
            get { return _stats; }
        }

        public void Initialize(IEnumerable<DicWord> allWords)
        {
            _stats = new StringBuilder();
            Dictionary<string, WordUsageInfo> topWords = LoadWords(_usageFile);

            var secondMatchWords = new Dictionary<string, List<WordUsageInfo>>();
            foreach (var topWord in topWords.Values)
            {
                string matchingKeyword = PrepareMatchingKeyword(topWord.Keyword, false);

                List<WordUsageInfo> items;
                if (!secondMatchWords.TryGetValue(matchingKeyword, out items))
                {
                    items = new List<WordUsageInfo>();
                    secondMatchWords.Add(matchingKeyword, items);
                }
                items.Add(topWord);
            }

            _words = new Dictionary<string, WordUsageInfo>();
            foreach (var word in allWords)
            {
                WordUsageInfo info = null;
                if (topWords.TryGetValue(word.Keyword, out info))
                {
                    info.IsMapped = true;
                    _words.Add(word.Keyword, info);
                }
            }

            // Second attempt
            foreach (var word in allWords)
            {
                string matchingKeyword = PrepareMatchingKeyword(word.Keyword, false);
                List<WordUsageInfo> items;
                if (secondMatchWords.TryGetValue(matchingKeyword, out items))
                {
                    WordUsageInfo usageInfo = items.OrderBy(x => x.CombinedRank).FirstOrDefault(x => !x.IsMapped);
                    if (usageInfo == null)
                    {
                        string caseMatchingKeyword = PrepareMatchingKeyword(word.Keyword, true);
                        usageInfo = items.OrderBy(x => x.CombinedRank)
                            .FirstOrDefault(x => PrepareMatchingKeyword(x.Keyword, true) == caseMatchingKeyword);
                    }

                    if (usageInfo == null)
                        continue;

                    WordUsageInfo previousMatch;
                    if (_words.TryGetValue(word.Keyword, out previousMatch))
                    {
                        if (usageInfo.CombinedRank < previousMatch.CombinedRank)
                        {
                            if (usageInfo.IsMapped)
                            {
                                _stats.AppendFormat("Ranks: reusing mapped rank '{0}'.\r\n", usageInfo.Keyword);
                            }

                            usageInfo.IsMapped = true;
                            previousMatch.IsMapped = false;
                            _words[word.Keyword] = usageInfo;

                            _stats.AppendFormat("Ranks: remapped keyword '{0}' to lower rank '{1}'.\r\n",
                                word.Keyword, usageInfo.Keyword);
                        }
                    }
                    else
                    {
                        if (usageInfo.IsMapped)
                        {
                            _stats.AppendFormat("Ranks: reusing mapped rank '{0}'.\r\n", usageInfo.Keyword);
                        }

                        usageInfo.IsMapped = true;
                        _words.Add(word.Keyword, usageInfo);

                        _stats.AppendFormat("Ranks: mapped keyword '{0}' to rank for '{1}' on second attempt.\r\n",
                            word.Keyword, usageInfo.Keyword);
                    }
                }
            }

            // Third attempt - consider that alternative spellings have the same rank (if they are not mapped yet)
            foreach (var word in allWords.Where(x => x.AlternativeSpellings != null && x.AlternativeSpellings.Count > 0))
            {
                WordUsageInfo usageInfo;
                if (!_words.TryGetValue(word.Keyword, out usageInfo))
                    continue;

                foreach (var altKeyword in word.AlternativeSpellings)
                {
                    if (!_words.ContainsKey(altKeyword))
                    {
                        _words.Add(altKeyword, usageInfo);
                        _stats.AppendFormat("Ranks: assinged alternative spelling keyword '{0}' the same rank as the original one '{1}'.\r\n",
                            altKeyword, word.Keyword);
                    }
                }
            }

            int count = 0;
            foreach (var keyword in topWords.Values.Where(x => !x.IsMapped).Select(x => x.Keyword).OrderBy(x => x))
            {
                count++;
                _stats.AppendFormat("Ranks: keyword '{0}' is not mapped.\r\n", keyword);
            }
            _stats.AppendFormat("Ranks: totally '{0}' keywords haven't been mapped.\r\n", count);
        }

        public void BuildUsageLists(IEnumerable<HtmlBuilder.WordGroup> groups)
        {
            _nodes = new Dictionary<string, WordListNode>();
            _nodeLists = new Dictionary<int, List<WordListNode>>();
            var lastNodes = new Dictionary<int, WordListNode>();
            foreach (var group in groups)
            {
                var secondaryNodes = new Dictionary<int, List<WordListNode>>();
                var mainNodes = new Dictionary<int, WordListNode>(); 
                foreach (var word in group.Words)
                {
                    WordUsageInfo info;
                    if (!_words.TryGetValue(word.Keyword, out info))
                        continue;

                    var node = new WordListNode{ PageName = group.Name, Keyword = word.Keyword, Rank = info.CombinedRank };
                    if (!mainNodes.ContainsKey(node.Rank))
                    {
                        mainNodes.Add(node.Rank, node);

                        WordListNode previousNode;
                        if (lastNodes.TryGetValue(node.Rank, out previousNode))
                        {
                            node.PreviousWord = previousNode;
                            previousNode.NextWord = node;
                        }
                        lastNodes[node.Rank] = node;
                    }
                    else
                    {
                        List<WordListNode> rankSecondary;
                        if (!secondaryNodes.TryGetValue(node.Rank, out rankSecondary))
                        {
                            rankSecondary = new List<WordListNode>();
                            secondaryNodes.Add(node.Rank, rankSecondary);
                        }
                        rankSecondary.Add(node);
                    }

                    _nodes.Add(node.Keyword, node);

                    List<WordListNode> rankNotes;
                    if (!_nodeLists.TryGetValue(node.Rank, out rankNotes))
                    {
                        rankNotes = new List<WordListNode>();
                        _nodeLists.Add(node.Rank, rankNotes);
                    }
                    rankNotes.Add(node);
                }

                foreach (var entry in secondaryNodes)
                {
                    var mainNode = mainNodes[entry.Key];
                    foreach (var secondaryNode in entry.Value)
                    {
                        secondaryNode.PreviousWord = mainNode.PreviousWord;
                        secondaryNode.NextWord = mainNode.NextWord;
                    }
                }
            }
        }

        public WordUsageInfo GetUsage(string keyword)
        {
            WordUsageInfo info;
            if (_words.TryGetValue(keyword, out info))
                return info;

            return null;
        }

        public int[] GetRanks()
        {
            return new[] { 1000, 2000, 3000, 6000, 9000 };
        }

        public WordListNode GetListNode(string keyword)
        {
            WordListNode node;
            _nodes.TryGetValue(keyword, out node);

            return node;
        }

        public List<WordListNode> GetRankNodes(int rank)
        {
            return _nodeLists[rank];
        }

        private string PrepareMatchingKeyword(string keyword, bool preserveCase)
        {
            if (CanRemoveDots.Contains(keyword))
            {
                keyword = keyword.Replace(".", "");
            }

            if (!preserveCase && keyword.Length > 1)
            {
                if (char.IsUpper(keyword[0]) && !char.IsUpper(keyword[1]))
                {
                    keyword = keyword.ToLower();
                }
            }

            return keyword;
        }

        private Dictionary<string, WordUsageInfo> LoadWords(string usageFile)
        {
            var words = new Dictionary<string, WordUsageInfo>();
            using (var source = new StreamReader(usageFile))
            {
                while (!source.EndOfStream)
                {
                    var parts = source.ReadLine().Split(new[] { '\t' });
                    if (parts.Length != 6)
                        throw new ArgumentException();

                    var keyword = parts[0];
                    var ranks = new WordRanks 
                    {
                        Longman = string.IsNullOrEmpty(parts[1]) ? 0 : int.Parse(parts[1]),
                        LongmanSpoken = string.IsNullOrEmpty(parts[2]) ? null : parts[2],
                        LongmanWritten = string.IsNullOrEmpty(parts[3]) ? null : parts[3],
                        COCA = string.IsNullOrEmpty(parts[4]) ? 0 : int.Parse(parts[4]),
                        IsAcademicWord = string.IsNullOrEmpty(parts[5]) ? false : bool.Parse(parts[5]),
                    };

                    WordUsageInfo info = new WordUsageInfo { Keyword = keyword, Ranks = ranks };
                    info.CombinedRank = info.Ranks.CalculateRank();
                    words.Add(keyword, info);
                }
            }

            return words;
        }
    }
}
