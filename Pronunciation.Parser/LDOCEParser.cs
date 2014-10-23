using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pronunciation.Parser
{
    class LDOCEParser
    {
        private readonly string _logFile;
        private readonly StringBuilder _log;
        private bool _ignoreLogRequests = false;
        private int _skippedCount = 0;
        private HashSet<string> _multiTranscriptions;
        private HashSet<string> _altWithTranscriptions;

        public const string TranscriptionNoteOpenTag = "<note>";
        public const string TranscriptionNoteCloseTag = "</note>";
        public const string TranscriptionItalicOpenTag = "<i>";
        public const string TranscriptionItalicCloseTag = "</i>";
        public const string TranscriptionSeparatorOpenTag = "<sp>";
        public const string TranscriptionSeparatorCloseTag = "</sp>";

        private const string TranscriptionReplacementOpenTag = "[tran]";
        private const string TranscriptionReplacementCloseTag = "[/tran]";

        private static string[] WrongAlternativeSpellings = new string[] { "have to do something", "Allhallowmas" };
        private static string[] AlternativeSpellingsToInject = new string[] { "Ms." };
        private static string[] WordsWithSeveralAudios = new string[] { "Zzz" };
        private static string[] ExplicitAmericanWords = new string[] { "color", "Ms." };
        private static string[] AllowedUsageMarks = new string[] { "AC", "S1", "S2", "S3", "W1", "W2", "W3" };
        private static EnglishVariant[] SingleVariants;
        private const string TheEnding = ", the";

        static LDOCEParser()
        {
            SingleVariants = (EnglishVariant[])Enum.GetValues(typeof(EnglishVariant));
        }

        public LDOCEParser(string logFile)
        {
            _logFile = logFile;
            _log = new StringBuilder();

            File.WriteAllText(logFile, DateTime.Now.ToString());
        }

        public LDOCEEntry[] Parse(string sourceFile)
        {
            Log("*** Started parsing ***\r\n");

            _multiTranscriptions = new HashSet<string>();
            _altWithTranscriptions = new HashSet<string>();

            var entries = new List<LDOCEEntry>();
            var alternativeItems = new List<LDOCEEntryItem>();
            LDOCEEntry entry = null;
            using (var reader = new StreamReader(sourceFile))
            {
                while (!reader.EndOfStream)
                {
                    var text = reader.ReadLine().Trim();
                    if (string.IsNullOrEmpty(text))
                        continue;

                    if (text.StartsWithOrdinal("#"))
                        continue;

                    LDOCEEntryItem item = null;
                    if (text.StartsWithOrdinal("{{"))
                    {
                        item = ParseNumberedItem(text, entry.Items.LastOrDefault());
                    }
                    else if (text.StartsWithOrdinal("["))
                    {
                        if (text.StartsWithOrdinal("[c blue]"))
                        {
                            item = ParseMainTag(null, text, entry.Items.LastOrDefault());
                        }
                        else if (text.StartsWithOrdinal("[m2]"))
                        {
                            ParseAmericanSpelling(text, entry.Items.LastOrDefault());
                        }
                        else
                            continue;
                    }
                    else if (text.StartsWithOrdinal("/ˌ") || text.StartsWithOrdinal("([i]"))
                    {
                        // Broken items: BR, Little Bear, Male, Daviyani
                        continue;
                    }
                    else
                    {
                        if (entry != null)
                        {
                            ProcessEntry(entry, entries, alternativeItems);
                        }
                        entry = new LDOCEEntry { Keyword = PrepareKeyword(text), Items = new List<LDOCEEntryItem>() };
                    }

                    if (item != null)
                    {
                        if (string.IsNullOrEmpty(item.ItemKeyword))
                        {
                            //Log("Entry without a text '{0}' {1}       {2}", entry.Keyword, item.ItemNumber, item.RawData);
                            continue;
                        }

                        if (item.ItemKeyword != entry.Keyword)
                        {
                            //Log("Entry with a different text '{0}' -> '{1}'", entry.Keyword, item.ItemText);
                            continue;
                        }

                        if (string.IsNullOrEmpty(item.Transcription)
                            && string.IsNullOrEmpty(item.SoundFileUK) && string.IsNullOrEmpty(item.SoundFileUS))
                        {
                            //Log("Empty entry '{0}' {1}", entry.Keyword, item.ItemNumber);
                            continue;
                        }

                        entry.Items.Add(item);  
                    }
                }
            }

            if (entry != null)
            {
                ProcessEntry(entry, entries, alternativeItems);
            }

            // Group entries by keyword (cut off ", the" ending)
            var finalEntries = new List<LDOCEEntry>();
            foreach (var group in entries.GroupBy(x => PrepareGroupingKeyword(x.Keyword)))
            {
                var mainEntry = new LDOCEEntry 
                { 
                    Keyword = group.Key,
                    Items = group.SelectMany(x => x.Items).ToList()
                };

                int itemNumber = 0;
                foreach (var item in mainEntry.Items)
                {
                    itemNumber++;
                    item.ItemNumber = itemNumber.ToString();
                }

                finalEntries.Add(mainEntry);
            }

            // Add entries with alternative spelling
            var skippedAlternatives = new List<string>();
            var addedAlternatives = new List<string>();
            foreach (var alternativeItem in alternativeItems)
            {
                string groupingKeyword = PrepareGroupingKeyword(alternativeItem.ItemKeyword);
                LDOCEEntry finalEntry = finalEntries.SingleOrDefault(x => x.Keyword == groupingKeyword);
                if (finalEntry == null)
                {
                    finalEntries.Add(new LDOCEEntry
                    {
                        Keyword = groupingKeyword,
                        IsDuplicate = true,
                        DuplicateSourceKeyword = alternativeItem.DuplicateSourceKeyword,
                        Items = new List<LDOCEEntryItem> { alternativeItem }
                    });

                    addedAlternatives.Add(string.Format(
                        "Added new alternative spelling entry '{0}' (original keyword '{1}')",
                        alternativeItem.ItemKeyword, alternativeItem.DuplicateSourceKeyword));
                }
                else if (AlternativeSpellingsToInject.Contains(alternativeItem.ItemKeyword))
                {
                    finalEntry.Items.Insert(0, alternativeItem);
   
                    addedAlternatives.Add(string.Format(
                        "Merged alternative spelling entry '{0}' (original item '{1}') with '{2}'",
                        alternativeItem.ItemKeyword, alternativeItem.DuplicateSourceKeyword, finalEntry.Keyword));
                }
                else
                {
                    skippedAlternatives.Add(string.Format(
                        "Skipped alternative spelling item '{0}' (original item '{1}')",
                        alternativeItem.ItemKeyword, alternativeItem.DuplicateSourceKeyword));
                }
            }

            foreach (var finalEntry in finalEntries.Where(x => x.Items.Any(y => y.ItemLanguage != null)))
            {
                var groupedItems = finalEntry.Items.GroupBy(x => x.ItemLanguage).ToArray();
                if (groupedItems.Length == 1 && groupedItems[0].Key != null)
                {
                    if (SingleVariants.Contains(groupedItems[0].Key.Value))
                    {
                        finalEntry.Language = groupedItems[0].Key;
                    }
                }
            }

            foreach (var keyword in ExplicitAmericanWords)
            {
                finalEntries.Single(x => x.Keyword == keyword).Language = EnglishVariant.American;
            }

            Log("\r\nADDED ALTERNATIVES:");
            Log(string.Join(Environment.NewLine, addedAlternatives.OrderBy(x => x)));

            Log("\r\nSKIPPED ALTERNATIVES");
            Log(string.Join(Environment.NewLine, skippedAlternatives.OrderBy(x => x)));

            Log("\r\nItems with more then one alternative spellings");
            Log(string.Join(Environment.NewLine, finalEntries.SelectMany(x => x.Items)
                .Where(x => x.AlternativeSpellings != null && x.AlternativeSpellings.Count > 1)
                .Select(x => string.Format("{0}: {1}", x.KeywordWithNumber, x.ItemTitle))
                .Distinct()));

            Log("\r\nItems with several transcriptions");
            Log(string.Join(Environment.NewLine, _multiTranscriptions.OrderBy(x => x)));

            Log("\r\nAlternative spellings with transcriptions (skipped)");
            Log(string.Join(Environment.NewLine, _altWithTranscriptions.OrderBy(x => x)));

            Log("\r\nTOTALS:");
            Log("Final entries: {0}", finalEntries.Count);
            Log("Skipped entries: {0}", _skippedCount);
            Log("Items without sound: {0}", finalEntries.SelectMany(x => x.Items)
                .Count(x => string.IsNullOrEmpty(x.SoundFileUK) && string.IsNullOrEmpty(x.SoundFileUS)));
            Log("Items without transcription: {0}", finalEntries.SelectMany(x => x.Items)
                .Count(x => string.IsNullOrEmpty(x.Transcription)));
            Log("Added alternative spelling entries: {0}", addedAlternatives.Count);
            Log("Skipped alternative spelling entries: {0}", skippedAlternatives.Count);

            Log("*** Ended parsing ***\r\n");
            File.AppendAllText(_logFile, _log.ToString());
            
            return finalEntries.ToArray();
        }

        private void ProcessEntry(LDOCEEntry entry, List<LDOCEEntry> entries, List<LDOCEEntryItem> alternativeItems)
        {
            if (entry == null || entry.Items.Count == 0)
            {
                _skippedCount++;
                Log("Skipped entry '{0}' because it's empty.", entry.Keyword);
                return;
            }

            // Check if all items have the same alternative spelling
            bool isAppliedToWholeEntry = entry.Items.GroupBy(x => BuildAltSpellingsGroupingKey(x.AlternativeSpellings))
                .Count() == 1;

            // Before merge we must prepare alternative entries
            var altItems = new List<LDOCEEntryItem>();
            foreach (var item in entry.Items.Where(x => x.AlternativeSpellings != null && x.AlternativeSpellings.Count > 0))
            {
                foreach (var alternativeSpelling in item.AlternativeSpellings)
                {
                    altItems.Add(new LDOCEEntryItem
                    {
                        ItemKeyword = alternativeSpelling.Keyword,
                        ItemLanguage = alternativeSpelling.Language,
                        ItemTitle = item.ItemTitle.Clone(),
                        Notes = item.Notes,
                        PartsOfSpeech = item.PartsOfSpeech == null ? null : new List<string>(item.PartsOfSpeech),
                        RawData = item.RawData,
                        SoundFileUK = item.SoundFileUK,
                        SoundFileUS = item.SoundFileUS,
                        Transcription = item.Transcription,
                        DuplicateSourceKeyword = isAppliedToWholeEntry ? item.ItemKeyword : null
                    });
                }
            }

            entry.Items = ProcessItems(entry.Items);
            entries.Add(entry);

            if (altItems.Count > 0)
            {
                alternativeItems.AddRange(ProcessItems(altItems));
            }
        }

        private List<LDOCEEntryItem> ProcessItems(List<LDOCEEntryItem> sourceItems)
        {
            var mergedItems = new List<LDOCEEntryItem>();
            foreach (var item in sourceItems)
            {
                LDOCEEntryItem matchedItem = null;
                if (string.IsNullOrEmpty(item.Transcription))
                {
                    matchedItem = mergedItems.LastOrDefault(x => x.ItemKeyword == item.ItemKeyword 
                        && string.IsNullOrEmpty(x.Transcription));
                    if (matchedItem != null)
                    {
                        if ((item.SoundFileUK != matchedItem.SoundFileUK || item.SoundFileUS != matchedItem.SoundFileUS)
                            && DifferByStress(item.ItemTitle, matchedItem.ItemTitle))
                        {
                            matchedItem = null;
                        }
                    }
                }
                else
                {
                    matchedItem = mergedItems.FirstOrDefault(x => x.ItemKeyword == item.ItemKeyword 
                        && x.Transcription == item.Transcription);
                }

                if (matchedItem == null)
                {
                    mergedItems.Add(item);
                }
                else
                {
                    MergeItems(matchedItem, item);
                }
            }

            return mergedItems;
        }

        private static string BuildAltSpellingsGroupingKey(List<LDOCEAlternativeSpelling> spellings)
        {
            if (spellings == null || spellings.Count == 0)
                return null;

            return string.Join("|", spellings.Select(x => x.Keyword));
        }

        private bool DifferByStress(DisplayName target, DisplayName source)
        {
            if (target == null || source == null)
                return false;

            if (target.ContainsStress && source.ContainsStress)
            {
                return !string.Equals(target.ToString(), source.ToString(), StringComparison.OrdinalIgnoreCase); 
            }

            return false;
        }

        private void MergeItems(LDOCEEntryItem target, LDOCEEntryItem source)
        {
            if (source == null || target == null)
                return;

            if (!string.IsNullOrEmpty(source.ItemNumber))
            {
                target.ItemNumber += (string.IsNullOrEmpty(target.ItemNumber) ? null : ", ") + source.ItemNumber;
            }

            if (source.PartsOfSpeech != null)
            {
                if (target.PartsOfSpeech == null)
                {
                    target.PartsOfSpeech = source.PartsOfSpeech;
                }
                else
                {
                    target.PartsOfSpeech.AddRange(source.PartsOfSpeech.Where(x => !target.PartsOfSpeech.Contains(x)));
                }
            }

            MergeTitles(target, source.ItemTitle);
        }

        private LDOCEEntryItem ParseNumberedItem(string text, LDOCEEntryItem previousItem)
        {
            var reader = new TagReader(text);
            if (!reader.LoadTagContent("{{Roman}}[b]", "[/b]{{/Roman}}", false))
                throw new Exception("Entry number is missing!");

            string itemNumber = reader.Content;
            if (string.IsNullOrEmpty(reader.RemainingText))
                throw new Exception("Entry number with empty content!");

            return ParseMainTag(itemNumber, reader.RemainingText.TrimStart(), previousItem);
        }

        // {{Roman}}[b]I.[/b]{{/Roman}} 	[c blue][b]-'s[/b][/c][sup]1[/sup] /z, s/ [p]BrE[/p] [s]bre_brelasde-!s.wav[/s] [p]AmE[/p] [s]ame_ld44-'s.wav[/s][/m]
        // [c blue][b]be[/b]‧[b]hav[/b]‧[b]iour[/b][/c][b][c red] S2[/c][/b][b][c red] W1[/c][/b] [p]BrE[/p] [s]bre_brelasdebehaviour.wav[/s] [p]AmE[/p] [s]ame_laadbehaviour.wav[/s][i][c maroon] British English[/c][/i], [b][c blue]behavior[/c][/b][i][c maroon] American English[/c][/i] /bɪˈheɪvjə $ -ər/[i][c] noun[/c][/i][c green] \[uncountable\][/c][/m]
        private LDOCEEntryItem ParseMainTag(string itemNumber, string text, LDOCEEntryItem previousItem)
        {
            if (!text.StartsWithOrdinal("[c blue][b]"))
                return null;

            text = ReplaceTranscriptionTags(text);
            var reader = new TagReader(text);
            if (!reader.LoadTagContent("[c blue][b]", "[/b][/c]", true))
                throw new ArgumentException();

            LDOCEEntryItem item = new LDOCEEntryItem { ItemNumber = itemNumber, RawData = text };
            item.ItemTitle = PrepareTitle(reader.Content);
            item.ItemKeyword = PrepareKeywordFromTitle(item.ItemTitle);

            int parsedNumber = 0;
            if (reader.LoadTagContent("[sup]", "[/sup]", false, true))
            {
                parsedNumber = int.Parse(Trim(reader.Content));
            }

            while (reader.LoadTagContent("[b][c red]", "[/c][/b]", false, true))
            {
                if (reader.IsInParentheses)
                    throw new ArgumentException();

                if (!AllowedUsageMarks.Contains(Trim(reader.Content)))
                    throw new ArgumentException();
            }

            var spellings = new List<LDOCEAlternativeSpelling>();
            while (true)
            {
                if (reader.LoadTagContent("[/m", "]", false, true))
                {
                    if (!string.IsNullOrEmpty(reader.Content))
                        throw new ArgumentException();

                    if (!string.IsNullOrEmpty(Trim(reader.RemainingText)))
                        throw new ArgumentException();

                    break;
                }

                if (reader.LoadTagContent(TranscriptionReplacementOpenTag, TranscriptionReplacementCloseTag, true, true))
                {
                    if (!reader.IsInParentheses)
                    {
                        string transcription = PrepareTranscription(Trim(reader.Content));
                        if (!string.IsNullOrEmpty(item.Transcription))
                        {
                            _multiTranscriptions.Add(string.Format("{0}: [{1}], [{2}]", item.ItemKeyword, item.Transcription, transcription));
                        }
                        else
                        {
                            item.Transcription = transcription;
                        }
                    }
                }
                else if (reader.LoadTagContent("[p]", "[/p]", false, true))
                {
                    if (!reader.IsInParentheses)
                    {
                        ParseSoundFile(reader, item);
                    }
                }
                else if (reader.LoadTagContent("[i][c]", "[/c][/i]", false, true))
                {
                    if (!reader.IsInParentheses)
                    {
                        item.PartsOfSpeech = SplitPartsOfSpeech(Trim(reader.Content));
                    }
                }
                else if (reader.LoadTagContent("[i][c maroon]", "[/c][/i]", false, true))
                {
                    if (!reader.IsInParentheses)
                    {
                        ParseNote(reader, item);
                    }
                }
                else if (reader.LoadTagContent("[b][c blue]", "[/c][/b]", false, true))
                {
                    if (!reader.IsInParentheses)
                    {
                        ParseAlternativeSpelling(reader, item, previousItem);
                    }
                }
                else if (reader.LoadTagContent("[c green]", "[/c]", true, true))
                {
                }
                else if (reader.LoadTagContent("[i]", "[/i]", false, true))
                {
                }
                else
                {
                    throw new ArgumentException();
                }
            }

            if (string.IsNullOrEmpty(item.Transcription) && previousItem != null)
            {
                item.Transcription = previousItem.Transcription;
            }

            return item;
        }
        
        private void ParseSoundFile(TagReader reader, LDOCEEntryItem item)
        {
            string lang = Trim(reader.Content);
            string fileName = null;
            if (reader.LoadTagContent("[s]", "[/s]", false, true))
            {
                fileName = Trim(reader.Content);
            }
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException();

            if (lang == "BrE")
            {
                if (!string.IsNullOrEmpty(item.SoundFileUK))
                {
                    if (!WordsWithSeveralAudios.Contains(item.ItemKeyword))
                        throw new ArgumentException();
                }
                else
                {
                    item.SoundFileUK = fileName;
                }
            }
            else if (lang == "AmE")
            {
                if (!string.IsNullOrEmpty(item.SoundFileUS))
                {
                    if (!WordsWithSeveralAudios.Contains(item.ItemKeyword))
                        throw new ArgumentException();
                }
                else
                {
                    item.SoundFileUS = fileName;
                }
            }
            else
            {
                throw new ArgumentException();
            }
        }

        private void ParseNote(TagReader reader, LDOCEEntryItem item)
        {
            var note = Trim(reader.Content);
            if (note == "trademark")
            {
                item.Notes = note;
            }
            else
            {
                EnglishVariant? language = ParseEnglishVariant(note);
                if (language != null)
                {
                    if (item.ItemLanguage == language)
                        throw new ArgumentException();

                    if (item.ItemLanguage != null)
                    {
                        item.ItemLanguage |= language;
                    }
                    else
                    {
                        item.ItemLanguage = language;
                    }
                }
            }
        }

        private string ReplaceTranscriptionTags(string text)
        {
            string result = text;
            var reader = new TagReader(text);
            while (reader.LoadTagContent(" /", new[] 
                { 
                    new ClosingTagInfo("/ "), 
                    new ClosingTagInfo("/, "), 
                    new ClosingTagInfo("/)"),
                    new ClosingTagInfo("/", "["), 
                },
                true))
            {
                if (string.IsNullOrEmpty(reader.Content) || reader.Content.Contains("(") || reader.Content.Contains(")"))
                    throw new ArgumentException();

                result = result.Replace(
                    string.Format("/{0}/", reader.Content),
                    string.Format("{0}{1}{2}", TranscriptionReplacementOpenTag, reader.Content, TranscriptionReplacementCloseTag));
            }

            return result;
        }

        private void ParseAlternativeSpelling(TagReader reader, LDOCEEntryItem item, LDOCEEntryItem previousItem)
        {
            // Possible patterns:
            // alt.spelling(s) -> main.trans.
            // alt.spelling(s) -> alt.note -> main.trans.
            // alt.spelling(s) -> alt.trans -> alt.note
            // alt.spelling(s) -> alt.note
            // alt.spelling(s)
            string transcription = null;
            bool alternativeEntryEnded = false;
            var allSpellings = new List<LDOCEAlternativeSpelling>();
            do
            {
                if (reader.IsInParentheses)
                    break;

                var words = new List<string>();
                var text = Trim(reader.Content);
                if (!text.EndsWith(", El") && !text.EndsWith(", the"))
                {
                    foreach (var part in text.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        words.Add(part);
                    }
                }
                else
                {
                    words.Add(text);
                }

                while (reader.LoadTagContent(TranscriptionReplacementOpenTag, TranscriptionReplacementCloseTag, true, true))
                {
                    if (reader.IsInParentheses)
                    {
                        alternativeEntryEnded = true;
                        break;
                    }

                    if (!string.IsNullOrEmpty(transcription))
                        throw new ArgumentException();

                    transcription = PrepareTranscription(Trim(reader.Content));
                }

                EnglishVariant? spellingLanguage = null;
                if (!alternativeEntryEnded)
                {
                    while (reader.LoadTagContent("[i][c maroon]", "[/c][/i]", false, true))
                    {
                        if (reader.IsInParentheses)
                        {
                            alternativeEntryEnded = true;
                            break;
                        }

                        EnglishVariant? language = ParseEnglishVariant(Trim(reader.Content));
                        if (language != null)
                        {
                            if (spellingLanguage != null)
                                throw new ArgumentException();

                            spellingLanguage = language;
                        }
                    }
                }

                // If transcription comes before language variant it means that that it's rather a different word
                if (spellingLanguage != null && !string.IsNullOrEmpty(transcription))
                {
                    _altWithTranscriptions.Add(string.Format("{0} -> {1}", item.ItemKeyword, string.Join("; ", words)));
                    break;
                }

                if (!alternativeEntryEnded)
                {
                    while (reader.LoadTagContent(TranscriptionReplacementOpenTag, TranscriptionReplacementCloseTag, true, true))
                    {
                        if (reader.IsInParentheses)
                        {
                            alternativeEntryEnded = true;
                            break;
                        }

                        if (!string.IsNullOrEmpty(transcription))
                            throw new ArgumentException();

                        transcription = PrepareTranscription(Trim(reader.Content));
                    }
                }

                foreach (var word in words)
                {
                    DisplayName title = PrepareTitle(word);
                    string keyword = PrepareKeywordFromTitle(title);
                    if (string.IsNullOrEmpty(keyword))
                        continue;

                    allSpellings.Add(new LDOCEAlternativeSpelling(keyword, title, spellingLanguage));
                }

                if (alternativeEntryEnded || !string.IsNullOrEmpty(transcription))
                    break;
            }
            while (reader.LoadTagContent("[b][c blue]", "[/c][/b]", false, true));

            allSpellings.RemoveAll(x => WrongAlternativeSpellings.Contains(x.Keyword));
            if (allSpellings.Count == 0)
                return;

            bool isStrictCheck = false;
            List<string> previousKeywords = null;
            if (string.IsNullOrEmpty(transcription))
            {
                isStrictCheck = true;
                if (previousItem != null && previousItem.AlternativeSpellings != null)
                {
                    previousKeywords = previousItem.AlternativeSpellings.Select(x => x.Keyword).ToList();
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(item.Transcription))
                {
                    // If alternative entry has a dedicated transcription it means this is not alternative spelling 
                    // but rather a different word
                    _altWithTranscriptions.Add(string.Format("Second check: {0} -> {1}",
                        item.ItemKeyword, string.Join("; ", allSpellings.Select(x => x.Keyword))));
                    return;
                }

                item.Transcription = transcription;                
            }

            foreach (var alternativeSpelling in allSpellings)
            {
                if (!IsAlternativeSpelling(item, alternativeSpelling, isStrictCheck, previousKeywords))
                    continue;

                if (item.AlternativeSpellings == null)
                {
                    item.AlternativeSpellings = new List<LDOCEAlternativeSpelling>();
                }
                item.AlternativeSpellings.Add(alternativeSpelling);
                MergeTitles(item, alternativeSpelling.Title);
            }
        }

        private bool IsAlternativeSpelling(LDOCEEntryItem item, LDOCEAlternativeSpelling alternativeSpelling, bool isStrictCheck, 
            List<string> keywordsFilter)
        {
            if (alternativeSpelling.Keyword == item.ItemKeyword)
                return false;

            if (alternativeSpelling.Keyword.StartsWithOrdinal("-") && !item.ItemKeyword.StartsWithOrdinal("-"))
            {
                if (alternativeSpelling.Keyword.Remove(0, 1) != item.ItemKeyword)
                    return false;
            }

            if (!isStrictCheck)
                return true;

            if (keywordsFilter != null && keywordsFilter.Contains(alternativeSpelling.Keyword))
                return true;

            string itemKeyword = RemoveSpecialSymbols(item.ItemKeyword);
            string altKeyword = RemoveSpecialSymbols(alternativeSpelling.Keyword);
            if (string.Equals(itemKeyword, altKeyword, StringComparison.OrdinalIgnoreCase))
                return true;

            if (item.ItemLanguage == EnglishVariant.British && alternativeSpelling.Language == EnglishVariant.American)
            {
                string convertedSpelling = itemKeyword
                    .Replace("grey", "gray")
                    .Replace("chequer", "checker")
                    .Replace("cheque", "check")
                    .Replace("centred", "centered")
                    .Replace("centre", "center")
                    .Replace("defence", "defense")
                    .Replace("dialogue", "dialog")
                    .Replace("aesthetic", "esthetic")
                    .Replace("mould", "mold")
                    .Replace("fibre", "fiber")
                    .Replace("licence", "license")
                    .Replace("programme", "program")
                    .Replace("tyre", "tire")
                    .Replace("anaemia", "anemia")
                    .Replace("whizz", "whiz")
                    .Replace("chilli", "chili")
                    .Replace("barrell", "barrel")
                    .Replace("travell", "travel")
                    .Replace("our", "or");
                if (string.Equals(convertedSpelling, altKeyword, StringComparison.OrdinalIgnoreCase))
                    return true;    
            }

            return false;
        }

        private EnglishVariant? ParseEnglishVariant(string wordNote)
        {
            EnglishVariant? language;
            switch (wordNote)
            {
                case "American English":
                    language = EnglishVariant.American;
                    break;
                case "British English":
                    language = EnglishVariant.British;
                    break;
                case "Australian English":
                    language = EnglishVariant.Australian;
                    break;
                default:
                    language = null;
                    break;
            }

            return language;
        }

        private bool ParseAmericanSpelling(string text, LDOCEEntryItem item)
        {
            string content = null;
            var reader = new TagReader(text);
            if (reader.LoadTagContent("[m2] the American spelling of ↑<<", ">>[/m]", false))
            {
                content = Trim(reader.Content);
            }
            else if (reader.LoadTagContent("[m2] the usual American spelling of ↑<<", ">>[/m]", false))
            {
                content = Trim(reader.Content);
            }

            if (string.IsNullOrEmpty(content))
                return false;

            if (item.AlternativeSpellings == null)
            {
                item.AlternativeSpellings = new List<LDOCEAlternativeSpelling>();
            }
            else if (item.AlternativeSpellings.Count > 0)
                throw new ArgumentException();

            DisplayName britishTitle = PrepareTitle(content);
            string britishKeyword = PrepareKeywordFromTitle(britishTitle);
            if (britishKeyword == item.ItemKeyword)
                throw new ArgumentException();

            if (item.ItemLanguage != null)
                throw new ArgumentException();

            item.ItemLanguage = EnglishVariant.American;

            // Put british title first
            DisplayName americanTitle = item.ItemTitle;
            item.ItemTitle = britishTitle;
            MergeTitles(item, americanTitle);

            return true;
        }

        private string RemoveSpecialSymbols(string target)
        {
            string result = null;
            foreach (var ch in target)
            {
                if (Char.IsLetterOrDigit(ch))
                {
                    result += ch;
                }
            }

            return result;
        }

        private void MergeTitles(LDOCEEntryItem target, DisplayName sourceTitle)
        {
            if (sourceTitle == null)
                return;

            if (target.ItemTitle == null)
            {
                target.ItemTitle = sourceTitle.Clone();
            }
            else
            {
                target.ItemTitle.Merge(sourceTitle);
            }
        }

        private string Trim(string text)
        {
            return string.IsNullOrEmpty(text) ? text : text.Trim();
        }

        private List<string> SplitPartsOfSpeech(string partsOfSpeech)
        {
            if (string.IsNullOrEmpty(partsOfSpeech))
                return null;

            return partsOfSpeech.Split(',').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList();
        }

        private void Log(string text)
        {
            Log(false, text, null);
        }

        private void Log(string format, params object[] args)
        {
            Log(false, format, args);
        }

        private void Log(bool forceLogging, string format, params object[] args)
        {
            if (_ignoreLogRequests && !forceLogging)
                return;

            if (args == null)
            {
                _log.Append(format);
            }
            else
            {
                _log.AppendFormat(format, args);
            }
            _log.AppendLine();
        }

        private DisplayName PrepareTitle(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            var titleText = text.Replace("[b]", "").Replace("[/b]", "").Replace("‧", "").Replace("·", "")
                .Replace("’", "'").Replace("ʌ", "");
            if (titleText.Contains("[") || titleText.Contains("]"))
                throw new ArgumentException();

            if (string.IsNullOrWhiteSpace(titleText))
                return null;

            return new DisplayName(Trim(titleText));
        }

        private string PrepareKeyword(string text)
        {
            return PrepareKeywordFromTitle(PrepareTitle(text));
        }

        private string PrepareKeywordFromTitle(DisplayName title)
        {
            if (title == null)
                return null;

            return Trim(title.GetStringWithoutStress());
        }

        private string PrepareGroupingKeyword(string keyword)
        {
            if (!keyword.ToLower().EndsWith(TheEnding))
                return keyword;

            return Trim(keyword.Substring(0, keyword.Length - TheEnding.Length));
        }

        private string PrepareTranscription(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            
            string result = text
                .Replace("[i][c gray]", TranscriptionNoteOpenTag)
                .Replace("[/c][/i]", TranscriptionNoteCloseTag)
                .Replace("[i]", TranscriptionItalicOpenTag)
                .Replace("[/i]", TranscriptionItalicCloseTag)
                .Replace(",", string.Format("{0},{1}", TranscriptionSeparatorOpenTag, TranscriptionSeparatorCloseTag));

            if (result.Contains("[") || result.Contains("]"))
                throw new Exception("Transcription contains unsupported tags inside!");

            return Trim(result);
        }
    }
}
