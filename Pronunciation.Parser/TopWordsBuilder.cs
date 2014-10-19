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
            public string Keyword;
            public List<TopWordForm> Forms;
        }

        private class TopWordForm
        {
            public string FormName;
            public SourceType Creator;
            public List<string> SpeechParts = new List<string>();
            public string LongmanRankS;
            public string LongmanRankW;
            public int? LongmanRank;
            public int? MacmillanRank;
            public int? CocaRank;
            public bool? IsAcademic;
        }

        private class MergedTopWordInfo
        {
            public string Keyword;
            public TopWordForm Form;
        }

        private enum SourceType
        {
            Undefined,
            Longman,
            Macmillan,
            COCA
        }

        public StringBuilder Log { get; private set; }

        public TopWordsBuilder()
        {
            Log = new StringBuilder();
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

        public void GenerateTopWordsWithPartsOfSpeech()
        {
            Dictionary<string, TopWordInfo> words = GroupWordsWithPartsOfSpeech();

            using (var dest = new StreamWriter(@"D:\WORK\NET\PronunciationTrainer\Data\TopWords\TopWordsWithPartsOfSpeech.txt"))
            {
                foreach (var word in words.Values.OrderBy(x => x.Keyword))
                {
                    foreach (var form in word.Forms.OrderBy(x =>
                        x.SpeechParts == null ? null : string.Join(", ", x.SpeechParts)))
                    {
                        dest.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}",
                            word.Keyword,
                            form.SpeechParts == null || form.SpeechParts.Count == 0 ? null : string.Join(", ", form.SpeechParts),
                            form.LongmanRank,
                            form.LongmanRankS,
                            form.LongmanRankW,
                            form.CocaRank,
                            form.IsAcademic);
                    }
                }
            }
        }

        public void GenerateTopWords()
        {
            Dictionary<string, TopWordInfo> words = GroupWordsWithPartsOfSpeech();

            // Merge word forms
            var mergedWords = new Dictionary<string, MergedTopWordInfo>();
            foreach (var word in words.Values.OrderBy(x => x.Keyword))
            {
                TopWordForm mergedForm = new TopWordForm();
                foreach (var form in word.Forms)
                {
                    MergeWordForms(mergedForm, form);
                }

                mergedWords.Add(word.Keyword, new MergedTopWordInfo { Keyword = word.Keyword, Form = mergedForm });
            }

            // Merge information from similar words
            foreach (var word in mergedWords.Values.OrderBy(x => x.Keyword))
            {
                string groupingKey = PrepareGroupingKey(word.Keyword);

                MergedTopWordInfo matchingWord;
                if (mergedWords.TryGetValue(groupingKey, out matchingWord))
                {
                    if (ReferenceEquals(matchingWord, word))
                    {
                        matchingWord = null;
                    }
                }

                if (matchingWord == null && word.Keyword.Contains("-"))
                {
                    groupingKey = word.Keyword.Replace("-", string.Empty);
                    if (mergedWords.TryGetValue(groupingKey, out matchingWord))
                    {
                        if (ReferenceEquals(matchingWord, word))
                        {
                            matchingWord = null;
                        }
                    }

                    if (matchingWord == null)
                    {
                        groupingKey = word.Keyword.Replace("-", " ");
                        mergedWords.TryGetValue(groupingKey, out matchingWord);
                    }
                }

                if (matchingWord != null && !ReferenceEquals(matchingWord, word) && matchingWord.Form.Creator != word.Form.Creator)
                {
                    MergeWordForms(matchingWord.Form, word.Form);
                    MergeWordForms(word.Form, matchingWord.Form);
                    Log.AppendFormat("Merged word '{0}' {1} with '{2}' {3}\r\n",
                        matchingWord.Keyword, matchingWord.Form.Creator, word.Keyword, word.Form.Creator);
                }
            }

            // Persist
            using (var dest = new StreamWriter(@"D:\WORK\NET\PronunciationTrainer\Data\TopWords\TopWords.txt"))
            {
                foreach (var word in mergedWords.Values.OrderBy(x => x.Keyword))
                {
                    dest.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}",
                        word.Keyword,
                        word.Form.LongmanRank,
                        word.Form.LongmanRankS,
                        word.Form.LongmanRankW,
                        word.Form.CocaRank,
                        word.Form.IsAcademic);
                }
            }
        }

        private string PrepareGroupingKey(string keyword)
        {
            if (keyword == "AM" || keyword == "A.M.")
            {
                keyword = "a.m.";
            }
            else if (keyword == "PM" || keyword == "P.M.")
            {
                keyword = "p.m.";
            }
            else if (keyword == "ok")
            {
                keyword = "OK";
            }
            else
            {
                keyword = keyword.Replace(".", "");
            }

            return keyword;
        }

        private void MergeWordForms(TopWordForm target, TopWordForm source)
        {
            if (target.Creator == SourceType.Undefined)
            {
                target.Creator = source.Creator;
            }
            target.LongmanRank = MergeNumberRank(target.LongmanRank, source.LongmanRank);
            target.LongmanRankS = MergeLongmanRank(target.LongmanRankS , source.LongmanRankS);
            target.LongmanRankW = MergeLongmanRank(target.LongmanRankW, source.LongmanRankW);
            target.MacmillanRank = MergeNumberRank(target.MacmillanRank, source.MacmillanRank);
            target.CocaRank = MergeNumberRank(target.CocaRank, source.CocaRank);
            if (source.IsAcademic == true)
            {
                target.IsAcademic = true;
            }
        }

        private string MergeLongmanRank(string targetRank, string sourceRank)
        {
            if (string.IsNullOrEmpty(sourceRank))
                return targetRank;

            if (string.IsNullOrEmpty(targetRank))
                return sourceRank;

            // Get second symbol which is a digit (S1, W2 etc.)
            return int.Parse(sourceRank.Substring(1)) < int.Parse(targetRank.Substring(1)) ? sourceRank : targetRank;
        }

        private int? MergeNumberRank(int? targetRank, int? sourceRank)
        {
            if (sourceRank == null)
                return targetRank;

            if (targetRank == null)
                return sourceRank;

            return sourceRank.Value < targetRank.Value ? sourceRank : targetRank;
        }

        private Dictionary<string, TopWordInfo> GroupWordsWithPartsOfSpeech()
        {
            var dic = new Dictionary<string, TopWordInfo>();

            MatchFile(@"D:\WORK\NET\PronunciationTrainer\Data\TopWords\TopLongman.txt", dic, SourceType.Longman);
            //MatchFile(@"D:\WORK\NET\PronunciationTrainer\Data\TopWords\TopMacmillan.txt", dic, SourceType.Macmillan);
            MatchFile(@"D:\WORK\NET\PronunciationTrainer\Data\TopWords\TopCOCA.txt", dic, SourceType.COCA);

            return dic;
        }

        public void GroupLongmanOriginal()
        {
            var dic = new Dictionary<string, TopWordForm>();
            PrepareLongmanFile("LongmanS1", (x) => x.LongmanRankS = "S1", dic);
            PrepareLongmanFile("LongmanS2", (x) => x.LongmanRankS = "S2", dic);
            PrepareLongmanFile("LongmanS3", (x) => x.LongmanRankS = "S3", dic);
            PrepareLongmanFile("LongmanW1", (x) => x.LongmanRankW = "W1", dic);
            PrepareLongmanFile("LongmanW2", (x) => x.LongmanRankW = "W2", dic);
            PrepareLongmanFile("LongmanW3", (x) => x.LongmanRankW = "W3", dic);
            PrepareLongmanFile("Longman3000", (x) => x.LongmanRank = 3000, dic);
            PrepareLongmanFile("Longman6000", (x) => x.LongmanRank = 6000, dic);
            PrepareLongmanFile("Longman9000", (x) => x.LongmanRank = 9000, dic);
            PrepareLongmanFile("LongmanAcademic", (x) => x.IsAcademic = true, dic);

            using (var dest = new StreamWriter(@"D:\WORK\NET\PronunciationTrainer\Data\TopWords\TopLongmanOriginal.txt"))
            {
                foreach (var form in dic.Values.OrderBy(x => x.FormName))
                {
                    dest.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}",
                        form.FormName,
                        form.LongmanRank,
                        form.LongmanRankS,
                        form.LongmanRankW,
                        form.IsAcademic);
                }
            }
        }

        public void ParseLongmanOriginal()
        {
            var partsOfSpeech = new string[] 
            { 
                "adjective",
                "adverb",
                "auxiliary verb",
                "conjunction",
                "definite article",
                "determiner",
                "indefinite article",
                "interjection",
                "modal verb",
                "noun",
                "number",
                "predeterminer",
                "preposition",
                "pronoun",
                "verb"
            };

            var replacements = new Dictionary<string, string>
            { 
                {"auxiliary verb", "verb"},
                {"definite article", "article"},
                {"indefinite article", "article"},
                {"modal verb", "verb"},
                {"predeterminer", "determiner"}
            };
            
            var forms = new Dictionary<string, TopWordForm>();
            using (var source = new StreamReader(@"D:\WORK\NET\PronunciationTrainer\Data\TopWords\TopLongmanOriginal.txt"))
            {
                while (!source.EndOfStream)
                {
                    var text = source.ReadLine();
                    if (string.IsNullOrEmpty(text))
                        throw new ArgumentException();

                    var parts = text.Split('\t');
                    if (parts.Length != 5)
                        throw new ArgumentException();

                    string combinedName = parts[0].Trim();
                    if (string.IsNullOrWhiteSpace(combinedName))
                        throw new ArgumentException();

                    TopWordForm form = ParseCombinedLongmanName(combinedName, partsOfSpeech);
                    form.LongmanRank = string.IsNullOrEmpty(parts[1]) ? (int?)null : int.Parse(parts[1]);
                    form.LongmanRankS = string.IsNullOrEmpty(parts[2]) ? null : parts[2];
                    form.LongmanRankW = string.IsNullOrEmpty(parts[3]) ? null : parts[3];
                    form.IsAcademic = string.IsNullOrEmpty(parts[4]) ? (bool?)null : bool.Parse(parts[4]);

                    form.SpeechParts = GroupLongmanSpeechParts(form.SpeechParts, replacements);
                    string groupingKey = form.SpeechParts == null || form.SpeechParts.Count == 0 
                        ? form.FormName
                        : string.Format("{0}|{1}", form.FormName, string.Join(",", form.SpeechParts));

                    TopWordForm finalForm;
                    if (forms.TryGetValue(groupingKey, out finalForm))
                    {
                        MergeWordForms(finalForm, form);
                        Log.AppendFormat("Merged '{0}'\r\n", groupingKey);
                    }
                    else
                    {
                        forms.Add(groupingKey, form);
                    }
                }
            }

            using (var dest = new StreamWriter(@"D:\WORK\NET\PronunciationTrainer\Data\TopWords\TopLongman.txt"))
            {
                foreach (var form in forms.Values.OrderBy(x => x.FormName))
                {
                    dest.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}",
                        form.FormName,
                        form.SpeechParts == null || form.SpeechParts.Count == 0 ? null : string.Join(", ", form.SpeechParts),
                        form.LongmanRank,
                        form.LongmanRankS,
                        form.LongmanRankW,
                        form.IsAcademic);
                }
            }
        }

        private List<string> GroupLongmanSpeechParts(List<string> speechParts, Dictionary<string, string> replacements)
        {
            if (speechParts == null || speechParts.Count == 0)
                return speechParts;

            var results = new List<string>();
            foreach(var speechPart in speechParts)
            {
                string replacement;
                if (replacements.TryGetValue(speechPart, out replacement))
                {
                    if (!results.Contains(replacement))
                    {
                        results.Add(replacement);
                    }
                }
                else
                {
                    if (!results.Contains(speechPart))
                    {
                        results.Add(speechPart);
                    }
                }
            }

            return results;
        }

        private TopWordForm ParseCombinedLongmanName(string combinedName, string[] speechParts)
        {
            var partsOfSpeech = new List<string>();
            string formName = null;
            int i = 0;
            foreach (var part in combinedName.Split(new string[] {", "}, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)))
            {
                if (i == 0)
                {
                    if (part.Contains(" "))
                    {
                        int splitIndex = -1;
                        for(int j = 0; j < part.Length; j++)
                        {
                            if (part[j] == ' ')
                            {
                                string speechPart = part.Substring(j + 1);
                                if (speechParts.Contains(speechPart))
                                {
                                    splitIndex = j;
                                    partsOfSpeech.Add(speechPart);
                                    break;
                                }
                            }
                        }

                        if (splitIndex < 0)
                        {
                            formName = part;
                        }
                        else
                        {
                            formName = part.Substring(0, splitIndex);
                        }
                    }
                    else
                    {
                        formName = part;
                    }
                }
                else
                {
                    if (!speechParts.Contains(part))
                        throw new ArgumentException();

                    partsOfSpeech.Add(part);
                }

                i++;
            }

            if (string.IsNullOrWhiteSpace(formName) || formName.Contains(","))
                throw new ArgumentException();

            formName = formName.Trim();
            int number;
            if (int.TryParse(formName.Last().ToString(), out number))
            {
                formName = formName.Remove(formName.Length - 1);
            }

            return new TopWordForm { FormName = formName, SpeechParts = partsOfSpeech };
        }

        private void PrepareLongmanFile(string sourceFileName, Action<TopWordForm> action, 
            Dictionary<string, TopWordForm> dic)
        {
            string sourceFile = string.Format(@"D:\WORK\NET\PronunciationTrainer\Data\TopWords\{0}.txt", sourceFileName);
            using (var source = new StreamReader(sourceFile))
            {
                while (!source.EndOfStream)
                {
                    var text = source.ReadLine().Trim();
                    if (string.IsNullOrEmpty(text))
                        continue;

                    TopWordForm form;
                    if (!dic.TryGetValue(text, out form))
                    {
                        form = new TopWordForm { FormName = text };
                        dic.Add(text, form);
                    }

                    action(form);
                }
            }
        }

        private void MatchFile(string sourceFile, Dictionary<string, TopWordInfo> dic, SourceType sourceType)
        {
            int columnsCount = (sourceType == SourceType.Longman ? 6 : 3);
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
                            Keyword = key,
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
                                if (form.LongmanRank != null)
                                    throw new ArgumentException();
                                form.LongmanRank = string.IsNullOrEmpty(parts[2]) ? (int?)null : int.Parse(parts[2]);

                                if (form.LongmanRankS != null)
                                    throw new ArgumentException();
                                form.LongmanRankS = parts[3];

                                if (form.LongmanRankW != null)
                                    throw new ArgumentException();
                                form.LongmanRankW = parts[4];

                                if (form.IsAcademic != null)
                                    throw new ArgumentException();
                                form.IsAcademic = string.IsNullOrEmpty(parts[5]) ? (bool?)null : bool.Parse(parts[5]);
                                break;

                            case SourceType.Macmillan:
                                if (form.MacmillanRank != null)
                                {
                                    int newRank = int.Parse(parts[2]);
                                    if (form.MacmillanRank.Value <= newRank)
                                    {
                                        Console.WriteLine("Skipped MC: {0} - {1}", info.Keyword,
                                             string.Join(", ", speechParts));
                                        continue;
                                    }
                                    else
                                    {
                                        Console.WriteLine("Overriden MC: {0} - {1} with {2}", info.Keyword,
                                            string.Join(", ", form.SpeechParts), string.Join(", ", speechParts));
                                    }
                                }

                                form.MacmillanRank = int.Parse(parts[2]);
                                break;

                            case SourceType.COCA:
                                if (form.CocaRank != null)
                                {
                                    int newRank = int.Parse(parts[2]);
                                    if (form.CocaRank.Value <= newRank)
                                    {
                                        Console.WriteLine("Skipped COCA: {0} - {1}", info.Keyword,
                                             string.Join(", ", speechParts));
                                        continue;
                                    }
                                    else
                                    {
                                        Console.WriteLine("Overriden COCA: {0} - {1} with {2}", info.Keyword,
                                            string.Join(", ", form.SpeechParts), string.Join(", ", speechParts));
                                    }
                                }

                                form.CocaRank = int.Parse(parts[2]);
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
