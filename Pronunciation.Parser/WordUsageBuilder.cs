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
            else if (LongmanSpoken == "S2" || LongmanWritten == "W2" || (COCA > 0 && COCA <= 2000))
            {
                rank = 2000;
            }
            else if (LongmanSpoken == "S3" || LongmanWritten == "W3" || (COCA > 0 && COCA <= 3000) || Macmillan == 2500)
            {
                rank = 3000;
            }
            else if ((COCA > 0 && COCA <= 5000) || Macmillan == 5000)
            {
                rank = 5000;
            }
            else if ((COCA > 0 && COCA <= 7500) || Macmillan == 7500)
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
        private Dictionary<string, WordUsageInfo> _words = new Dictionary<string, WordUsageInfo>();
        private string _usageFile;

        public WordUsageBuilder(string usageFile)
        {
            if (string.IsNullOrEmpty(usageFile))
                throw new ArgumentNullException();

            _usageFile = usageFile;
        }

        public void Initialize(IEnumerable<string> keys)
        {
            var keywords = keys.ToDictionary(x => x);

            // Build rank
            using (var source = new StreamReader(_usageFile))
            {
                while (!source.EndOfStream)
                {
                    var parts = source.ReadLine().Split(new[] { '\t' });
                    if (parts.Length != 6)
                        throw new ArgumentException();

                    var keyword = parts[0];
                    if (!keywords.ContainsKey(keyword))
                        continue;

                    WordUsageInfo info;
                    if (!_words.TryGetValue(keyword, out info))
                    {
                        info = new WordUsageInfo { Keyword = keyword };
                        _words.Add(keyword, info);
                    }

                    var ranks = new WordRanks 
                    {
                        LongmanSpoken = parts[2],
                        LongmanWritten = parts[3],
                        Macmillan = string.IsNullOrEmpty(parts[4]) ? 0 : int.Parse(parts[4]),
                        COCA = string.IsNullOrEmpty(parts[5]) ? 0 : int.Parse(parts[5])
                    };
                    var rank = ranks.CalculateRank();
                    if (rank < info.CombinedRank || info.CombinedRank == 0)
                    {
                        info.CombinedRank = rank;
                        info.Ranks = ranks;
                    }
                }
            }

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
