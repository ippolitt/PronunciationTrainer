using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pronunciation
{
    class WordUsageInfo
    {
        public string Keyword;
        public int Rank;
        public WordUsageInfo PreviousWord;
        public WordUsageInfo NextWord;
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

                    var rank = CalculateRank(parts[2], parts[3], parts[4], parts[5]);
                    if (rank < info.Rank || info.Rank == 0)
                    {
                        info.Rank = rank;
                    }
                }
            }

            // Build previous and next words within each rank
            foreach (var group in _words.Values.GroupBy(x => x.Rank))
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
            return _words.Values.Where(x => x.Rank == rank);
        }

        private int CalculateRank(string longmanS, string longmanW, string macmillan, string coca)
        {
            int cocaRank = string.IsNullOrEmpty(coca) ? 0 : int.Parse(coca);

            int rank;
            if (longmanS == "S1" || longmanW == "W1" || (cocaRank > 0 && cocaRank <= 1000))
            {
                rank = 1000;
            }
            else if (longmanS == "S2" || longmanW == "W2" || (cocaRank > 0 && cocaRank <= 2000))
            {
                rank = 2000;
            }
            else if (longmanS == "S3" || longmanW == "W3" || (cocaRank > 0 && cocaRank <= 3000) || macmillan == "2500")
            {
                rank = 3000;
            }
            else if ((cocaRank > 0 && cocaRank <= 5000) || macmillan == "5000")
            {
                rank = 5000;
            }
            else if ((cocaRank > 0 && cocaRank <= 7500) || macmillan == "7500")
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
}
