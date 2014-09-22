using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pronunciation.Parser
{
    class TopWordsBuilder
    {
        private class TopWordInfo
        {
            public string Word;
            public List<TopWordForm> Forms;
        }

        private class TopWordForm
        {
            public SourceType Creator;
            public List<string> SpeechParts = new List<string>();
            public string LongmanRankS;
            public string LongmanRankW;
            public string MacmillanRank;
            public string CocaRank;
        }

        private enum SourceType
        {
            Longman,
            Macmillan,
            COCA
        }

        public void GroupWords()
        {
            GroupWords(@"D:\WORK\NET\PronunciationTrainer\Data\TopWords\TopLongman.txt", SourceType.Longman);
            GroupWords(@"D:\WORK\NET\PronunciationTrainer\Data\TopWords\TopMacmillan.txt", SourceType.Macmillan);
            GroupWords(@"D:\WORK\NET\PronunciationTrainer\Data\TopWords\TopCOCA.txt", SourceType.COCA);
        }

        private void GroupWords(string sourceFile, SourceType sourceType)
        {
            var dic = new Dictionary<string, string>();
            int total = 0;
            using (var source = new StreamReader(sourceFile))
            {
                while (!source.EndOfStream)
                {
                    var parts = source.ReadLine().Split(new[] { '\t' });
                    var key = parts[0];

                    if (!dic.ContainsKey(key))
                    {
                        dic.Add(key, null);
                    }
                    total++;
                }
            }

            Console.WriteLine("{0}: total - {1}, unique - {2}.", sourceType, total, dic.Count);
        }

        public void MergeTopWords()
        {
            var dic = new Dictionary<string, TopWordInfo>();

            MatchFile(@"D:\WORK\NET\PronunciationTrainer\Data\TopWords\TopLongman.txt", dic, SourceType.Longman);
            MatchFile(@"D:\WORK\NET\PronunciationTrainer\Data\TopWords\TopMacmillan.txt", dic, SourceType.Macmillan);
            MatchFile(@"D:\WORK\NET\PronunciationTrainer\Data\TopWords\TopCOCA.txt", dic, SourceType.COCA);

            using (var dest = new StreamWriter(@"D:\WORK\NET\PronunciationTrainer\Data\TopWords\TopWords.txt"))
            {
                foreach (var word in dic.Values.OrderBy(x => x.Word))
                {
                    foreach (var form in word.Forms.OrderBy(x =>
                        x.SpeechParts == null ? null : string.Join(", ", x.SpeechParts)))
                    {
                        dest.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", 
                            word.Word,
                            form.SpeechParts == null ? null : string.Join(", ", form.SpeechParts),
                            form.LongmanRankS,
                            form.LongmanRankW,
                            form.MacmillanRank,
                            form.CocaRank);
                    }
                }
            }
        }

        private void MatchFile(string sourceFile, Dictionary<string, TopWordInfo> dic, SourceType sourceType)
        {
            int columnsCount = (sourceType == SourceType.Longman ? 4 : 3);
            using (var source = new StreamReader(sourceFile))
            {
                while (!source.EndOfStream)
                {
                    var parts = source.ReadLine().Split(new[] { '\t' });
                    if (parts.Length != columnsCount)
                        throw new ArgumentException();

                    var key = parts[0];
                    var speechParts = GetSpeechParts(parts[1]);

                    TopWordInfo info;
                    List<TopWordForm> matchedForms = new List<TopWordForm>();
                    if (!dic.TryGetValue(key, out info))
                    {
                        info = new TopWordInfo
                        {
                            Word = key,
                            Forms = new List<TopWordForm> 
                            { 
                                new TopWordForm { Creator = sourceType, SpeechParts = speechParts }
                            }
                        };
                        dic.Add(key, info);

                        matchedForms.AddRange(info.Forms);
                    }
                    else
                    {
                        if (speechParts.Count == 0)
                        {
                            var form = info.Forms.SingleOrDefault(x => x.SpeechParts.Count == 0);
                            if (form != null)
                            {
                                matchedForms.Add(form);
                            }
                        }
                        else
                        {
                            foreach (var speechPart in speechParts)
                            {
                                var form = info.Forms.SingleOrDefault(x => x.SpeechParts.Contains(speechPart));
                                if (form != null)
                                {
                                    //Console.WriteLine("{0} {1} {2}", info.Word, string.Join(", ", form.SpeechParts), string.Join(", ", speechParts));
                                    //continue;
                                    if (form.Creator == sourceType)
                                        throw new ArgumentException();

                                    if (!matchedForms.Contains(form))
                                    {
                                        matchedForms.Add(form);
                                    }
                                }
                            }
                        }

                        if (matchedForms.Count <= 0)
                        {
                            var form = new TopWordForm { Creator = sourceType, SpeechParts = speechParts };
                            info.Forms.Add(form);
                            matchedForms.Add(form);
                        }
                    }

                    foreach (var form in matchedForms)
                    {
                        switch (sourceType)
                        {
                            case SourceType.Longman:
                                if (form.LongmanRankS != null)
                                    throw new ArgumentException();
                                form.LongmanRankS = parts[2];

                                if (form.LongmanRankW != null)
                                    throw new ArgumentException();
                                form.LongmanRankW = parts[3];
                                break;

                            case SourceType.Macmillan:
                                if (form.MacmillanRank != null)
                                {
                                    int oldRank = int.Parse(form.MacmillanRank);
                                    int newRank = int.Parse(parts[2]);
                                    if (oldRank <= newRank)
                                    {
                                        Console.WriteLine("Skipped MC: {0} - {1}", info.Word,
                                             string.Join(", ", speechParts));
                                        continue;
                                    }
                                    else
                                    {
                                        Console.WriteLine("Overriden MC: {0} - {1} with {2}", info.Word,
                                            string.Join(", ", form.SpeechParts), string.Join(", ", speechParts));
                                    }
                                }

                                form.MacmillanRank = parts[2];
                                break;

                            case SourceType.COCA:
                                if (form.CocaRank != null)
                                {
                                    int oldRank = int.Parse(form.CocaRank);
                                    int newRank = int.Parse(parts[2]);
                                    if (oldRank <= newRank)
                                    {
                                        Console.WriteLine("Skipped COCA: {0} - {1}", info.Word,
                                             string.Join(", ", speechParts));
                                        continue;
                                    }
                                    else
                                    {
                                        Console.WriteLine("Overriden COCA: {0} - {1} with {2}", info.Word,
                                            string.Join(", ", form.SpeechParts), string.Join(", ", speechParts));
                                    }
                                }

                                form.CocaRank = parts[2];
                                break;
                        }
                    }
                }
            }
        }

        private List<string> GetSpeechParts(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new List<string>();

            return text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
        }

        public void BuildTopWords()
        {
            using (var source = new StreamReader(@"D:\WORK\NET\PronunciationTrainer\Data\TopWords\TopWords.txt"))
            {
                using (var dest = new StreamWriter(@"D:\WORK\NET\PronunciationTrainer\Data\TopWords\TopWords1.txt"))
                {
                    while (!source.EndOfStream)
                    {
                        var parts = source.ReadLine().Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 2)
                            throw new ArgumentException();

                        string spoken = null;
                        string written = null;
                        string desc = null;
                        foreach (var col in parts.Where((x, i) => i > 0))
                        {
                            var s = col.Replace(",", " ").Trim();
                            switch (s)
                            {
                                case "S1":
                                case "S2":
                                case "S3":
                                    if (spoken != null)
                                        throw new ArgumentException();

                                    spoken = s;
                                    break;
                                case "W1":
                                case "W2":
                                case "W3":
                                    if (written != null)
                                        throw new ArgumentException();

                                    written = s;
                                    break;
                                default:
                                    desc += (string.IsNullOrEmpty(desc) ? null : " ") + col;
                                    break;
                            }
                        }

                        if (spoken == null && written == null)
                            throw new ArgumentException();

                        if (string.IsNullOrEmpty(desc))
                        {
                        }

                        dest.Write("{0}\t{1}\t{2}\t{3}\r\n", parts[0], desc, spoken, written);

                    }
                }
            }
        }

        public void FixMacmillan()
        {
            var ends = new string[]
            { 
                "abbreviation",
                "adjective", 
                "adverb", 
                "conjunction", 
                "determiner",
                "interjection",
                "modal verb",
                "noun",
                "number",
                "predeterminer",
                "preposition",
                "pronoun",  
                "short form",
                "verb"        
            };

            var wrongWords = new string [] {"le", "lo", "p", "ple", "re", "spre", "tre" };

            StringBuilder bld = new StringBuilder();
            foreach (var record in File.ReadAllLines(@"D:\WORK\NET\PronunciationTrainer\Data\TopWords\TopMacmillan_old.txt"))
            {
                var parts = record.Split('\t');
                if (parts.Length != 3)
                    throw new ArgumentException();

                string keyword;
                string speechPart;
                if (string.IsNullOrEmpty(parts[1]))
                {
                    keyword = parts[0];
                    speechPart = null;
                    if (keyword == "Mr" || keyword == "Mrs" || keyword == "Ms")
                    {
                        speechPart = "noun";
                    }
                }
                else
                {
                    if (parts[1].Contains(','))
                    {
                        keyword = parts[0];
                        speechPart = parts[1];
                    }
                    else
                    {
                        var combined = parts[0] + parts[1];
                        var matchedEndings = ends.Where(x => combined.EndsWith(x, StringComparison.OrdinalIgnoreCase))
                            .OrderByDescending(x => x.Length).ToList();
 
                        string matchedEnding = "s";
                        if (matchedEndings.Count == 1)
                        {
                            matchedEnding = matchedEndings[0];
                        }
                        else if (matchedEndings.Count == 2)
                        {
                            matchedEnding = matchedEndings[0];
                            keyword = combined.Substring(0, combined.Length - matchedEnding.Length);
                            if (wrongWords.Contains(keyword))
                            {
                                matchedEnding = matchedEndings[1];
                            }
                        }
                        else
                            throw new ArgumentException();

                        keyword = combined.Substring(0, combined.Length - matchedEnding.Length);
                        speechPart = combined.Substring(keyword.Length);
                    }
                }

                bld.AppendFormat("{0}\t{1}\t{2}\r\n", keyword, speechPart, parts[2]);
            }

            File.WriteAllText(@"D:\WORK\NET\PronunciationTrainer\Data\TopWords\TopMacmillan.txt", bld.ToString());
        }

        public void BuildMacmillan()
        {
            var endsPriority = new List<string> { 
                "modal verb",
                "predeterminer",
                "pronoun"};

            var endsOther = new List<string> { 
                "abbreviation",
                "adjective", 
                "adverb", 
                "conjunction", 
                "determiner",
                "interjection",
                "noun",
                "number",
                "preposition",
                "short form",
                "verb"};

            var suffixes = new string[] { "2500", "5000", "7500" };

            using (var dest = new StreamWriter(@"D:\WORK\NET\PronunciationTrainer\Data\TopWords\TopMacmillan.txt", false, Encoding.Default))
            {
                var missing = new List<string>();
                foreach (var suffix in suffixes)
                {
                    using (var source = new StreamReader(string.Format(
                        @"D:\WORK\NET\PronunciationTrainer\Data\TopWords\TopMac{0}.txt", suffix), Encoding.Default))
                    {
                        while (!source.EndOfStream)
                        {
                            var entry = source.ReadLine().Trim();
                            var parts = entry.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                            var ending = MatchEnding(parts[0], endsPriority);
                            if (ending == null)
                            {
                                ending = MatchEnding(parts[0], endsOther);
                            }

                            string keyword;
                            string desc;
                            if (ending == null)
                            {
                                missing.Add(entry);

                                keyword = entry;
                                desc = null;
                            }
                            else
                            {
                                keyword = parts[0].Remove(parts[0].Length - ending.Length).Trim();
                                desc = ending;
                                foreach (var col in parts.Where((x, i) => i > 0))
                                {
                                    desc += ", " + col.Trim();
                                }
                            }

                            dest.Write("{0}\t{1}\tM{2}\r\n", keyword, desc, suffix);
                        }
                    }
                }

                if (missing.Count > 0)
                {
                    File.WriteAllLines(@"D:\WORK\NET\PronunciationTrainer\Data\TopWords\TopMissing.txt", missing);
                }
            }
        }

        public void NormalizeLongman()
        {
            var map = new Dictionary<string, string> { 
                {"indefinite article", "indefinite article"},
                {"definite article", "definite article"},
                {"determiner", "determiner"},
                {"predeterminer", "predeterminer"},
                {"adj", "adjective"}, 
                {"adv", "adverb"}, 
                {"conj", "conjunction"}, 
                {"interjection", "interjection"},
                {"n", "noun"},
                {"number", "number"},
                {"prep", "preposition"},
                {"pron", "pronoun"},
                {"v", "verb"},
                {"auxiliary", "auxiliary verb"},
                {"modal", "modal verb"}};

            using (var source = new StreamReader(@"D:\WORK\NET\PronunciationTrainer\Data\TopWords\TopLongman.txt"))
            {
                using (var dest = new StreamWriter(@"D:\WORK\NET\PronunciationTrainer\Data\TopWords\TopLongman1.txt"))
                {
                    while (!source.EndOfStream)
                    {
                        var parts = source.ReadLine().Split(new[] { '\t' });
                        if (parts.Length != 4)
                            throw new ArgumentException();

                        string transformed = null;
                        foreach (var part in parts[1].Split(','))
                        {
                            if (string.IsNullOrEmpty(part))
                                continue;

                            var mapped = map[part.Trim()];
                            if (mapped == null)
                                throw new ArgumentException();

                            transformed += (string.IsNullOrEmpty(transformed) ? null : ", ") + mapped;
                        }

                        dest.Write("{0}\t{1}\t{2}\t{3}\r\n", parts[0], transformed, parts[2], parts[3]);
                    }
                }
            }
        }

        public void NormalizeCOCA()
        {
            var map = new Dictionary<string, string> { 
                {"a", "article"},
                {"c", "conjunction"}, 
                {"d", "determiner"},
                {"i", "preposition"},
                {"j", "adjective"}, 
                {"m", "number"},
                {"n", "noun"},
                {"p", "pronoun"},
                {"r", "adverb"}, 
                {"u", "interjection"},
                {"v", "verb"},
                {"t", null},
                {"e", "pronoun"},
                {"x", "adverb"}};

            using (var source = new StreamReader(@"D:\WORK\NET\PronunciationTrainer\Data\TopWords\TopCOCA.txt", Encoding.Default))
            {
                using (var dest = new StreamWriter(@"D:\WORK\NET\PronunciationTrainer\Data\TopWords\TopCOCA1.txt"))
                {
                    while (!source.EndOfStream)
                    {
                        var parts = source.ReadLine().Split(new[] { '\t' });
                        if (parts.Length != 5)
                            throw new ArgumentException();

                        string transformed = map[parts[2].Trim()];

                        dest.Write("{0}\t{1}\t{2}\t{3}\t{4}\r\n", parts[0], parts[1].Trim(), transformed, parts[3], parts[4]);
                    }
                }
            }
        }

        private static string MatchEnding(string s, List<string> endings)
        {
            if (string.IsNullOrEmpty(s))
                throw new ArgumentNullException();

            foreach (var ending in endings)
            {
                if (s.EndsWith(ending))
                    return ending;
            }

            return null;

        }
    }
}
