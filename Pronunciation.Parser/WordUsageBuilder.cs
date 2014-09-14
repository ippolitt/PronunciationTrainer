using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pronunciation.Parser
{
    class WordUsageInfo
    {
        public string Keyword;
        public int CombinedRank;
        public WordRanks Ranks;
        public WordUsageInfo PreviousWord;
        public WordUsageInfo NextWord;
    }

    public class WordRanks
    {
        public string LongmanSpoken;
        public string LongmanWritten;
        public int Macmillan;
        public int COCA;

        public int CalculateRank()
        {
            int rank;
            if (LongmanSpoken == "S1" || LongmanWritten == "W1" || (COCA > 0 && COCA <= 1000))
            {
                rank = 1000;
            }
            else if (LongmanSpoken == "S2" || LongmanWritten == "W2" || (COCA > 1000 && COCA <= 2000))
            {
                rank = 2000;
            }
            else if (LongmanSpoken == "S3" || LongmanWritten == "W3" || (COCA > 2000 && COCA <= 3000) || Macmillan == 2500)
            {
                rank = 3000;
            }
            else if ((COCA > 3000 && COCA <= 5000) || Macmillan == 5000)
            {
                rank = 5000;
            }
            else if ((COCA > 5000 && COCA <= 7500) || Macmillan == 7500)
            {
                rank = 7500;
            }
            else
            {
                throw new ArgumentException();
            }

            return rank;
        }
    }

    class WordUsageBuilder
    {
        private Dictionary<string, WordUsageInfo> _words;
        private string _usageFile;
        private StringBuilder _stats;

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

        public void Initialize(IEnumerable<string> keywords)
        {
            _stats = new StringBuilder();
            Dictionary<string, WordUsageInfo> wordsWithRank = GroupWords(_usageFile);

            _words = new Dictionary<string, WordUsageInfo>();
            foreach (var keyword in keywords)
            {
                WordUsageInfo info;
                if (wordsWithRank.TryGetValue(keyword, out info))
                {
                    info.CombinedRank = info.Ranks.CalculateRank();
                    _words.Add(keyword, info);
                }
            }

            int count = 0;
            foreach (var keyword in wordsWithRank.Keys.Where(x => !_words.ContainsKey(x)).OrderBy(x => x))
            {
                count++;
                _stats.AppendFormat("Keyword '{0}' is not mapped.\r\n", keyword);
            }
            _stats.AppendFormat("Totally '{0}' keywords haven't been mapped.\r\n", count);

            // Build previous and next words within each rank
            foreach (var group in _words.Values.GroupBy(x => x.CombinedRank))
            {
                WordUsageInfo previous = null;
                foreach (var word in group.OrderBy(x => x.Keyword))
                {
                    if (previous != null)
                    {
                        word.PreviousWord = previous;
                        previous.NextWord = word;
                    }

                    previous = word;
                }
            }
        }

        private Dictionary<string, WordUsageInfo> GroupWords(string usageFile)
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
                        LongmanSpoken = parts[2],
                        LongmanWritten = parts[3],
                        Macmillan = string.IsNullOrEmpty(parts[4]) ? 0 : int.Parse(parts[4]),
                        COCA = string.IsNullOrEmpty(parts[5]) ? 0 : int.Parse(parts[5])
                    };

                    WordUsageInfo info;
                    if (!words.TryGetValue(keyword, out info))
                    {
                        info = new WordUsageInfo { Keyword = keyword, Ranks = ranks };
                        words.Add(keyword, info);
                    }
                    else
                    {
                        MergeRanks(info.Ranks, ranks);
                    }
                }
            }

            return words;
        }

        private void MergeRanks(WordRanks target, WordRanks source)
        {
            if (LongmanRankIsHigher(source.LongmanSpoken, target.LongmanSpoken))
            {
                target.LongmanSpoken = source.LongmanSpoken;
            }

            if (LongmanRankIsHigher(source.LongmanWritten, target.LongmanWritten))
            {
                target.LongmanWritten = source.LongmanWritten;
            }

            if (source.Macmillan > 0 && (target.Macmillan <= 0 || source.Macmillan < target.Macmillan))
            {
                target.Macmillan = source.Macmillan;
            }

            if (source.COCA > 0 && (target.COCA <= 0 || source.COCA < target.COCA))
            {
                target.COCA = source.COCA;
            }
        }

        // S1, W2 etc
        private bool LongmanRankIsHigher(string sourceRank, string targetRank)
        {
            if (string.IsNullOrEmpty(sourceRank))
                return false;

            if (string.IsNullOrEmpty(targetRank))
                return true;

            return (int.Parse(sourceRank.Substring(1)) < int.Parse(targetRank.Substring(1)));
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
            return new[] { 1000, 2000, 3000, 5000, 7500 };
        }

        public IEnumerable<WordUsageInfo> GetWords(int rank)
        {
            return _words.Values.Where(x => x.CombinedRank == rank);
        }
    }
}
