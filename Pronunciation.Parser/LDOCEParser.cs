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

        public const string TranscriptionNoteOpenTag = "<note>";
        public const string TranscriptionNoteCloseTag = "</note>";
        public const string TranscriptionItalicOpenTag = "<i>";
        public const string TranscriptionItalicCloseTag = "</i>";
        public const string TranscriptionSeparatorOpenTag = "<sp>";
        public const string TranscriptionSeparatorCloseTag = "</sp>";

        private const string TheEnding = ", the";

        public LDOCEParser(string logFile)
        {
            _logFile = logFile;
            _log = new StringBuilder();

            File.WriteAllText(logFile, DateTime.Now.ToString());
        }

        public LDOCEEntry[] Parse(string sourceFile)
        {
            Log("*** Started parsing ***\r\n");

            var entries = new List<LDOCEEntry>();
            LDOCEEntry entry = null;
            using (var reader = new StreamReader(sourceFile))
            {
                while (!reader.EndOfStream)
                {
                    var text = reader.ReadLine().Trim();
                    if (string.IsNullOrEmpty(text))
                        continue;

                    if (text.StartsWith("#"))
                        continue;

                    LDOCEEntryItem item = null;
                    if (text.StartsWith("{{"))
                    {
                        item = ParseNumberedItem(text);
                    }
                    else if (text.StartsWith("["))
                    {
                        if (!text.StartsWith("[c blue]"))
                            continue;

                        item = ParseMainTag(null, text);
                    }
                    else if (text.StartsWith("/ˌ") || text.StartsWith("([i]"))
                    {
                        // Broken items: BR, Little Bear, Male, Daviyani
                        continue;
                    }
                    else
                    {
                        if (entry != null)
                        {
                            ProcessEntry(entry, entries);
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
                ProcessEntry(entry, entries);
            }

            // Group entries by keyword (cut off ", the" ending)
            var finalEntries = new List<LDOCEEntry>();
            var finalKeywords = new HashSet<string>();
            foreach (var group in entries.GroupBy(x => PrepareGroupingKeyword(x.Keyword)))
            {
                var mainEntry = new LDOCEEntry 
                { 
                    Keyword = group.Key, 
                    Items = group.SelectMany(x => x.Items).OrderBy(x => x.ItemKeyword).ToList()
                };

                int itemNumber = 0;
                foreach (var item in mainEntry.Items)
                {
                    itemNumber++;
                    item.ItemNumber = itemNumber.ToString();
                }

                finalEntries.Add(mainEntry);
                finalKeywords.Add(mainEntry.Keyword);
            }

            // Add entries with alternative spelling
            var ignoredItems = new List<LDOCEEntryItem>();
            var extraItems = finalEntries.SelectMany(x => x.Items)
                .Where(x => !string.IsNullOrEmpty(x.AlternativeSpelling)).ToArray();
            foreach (var item in extraItems)
            {
                string groupingKeyword = PrepareGroupingKeyword(item.AlternativeSpelling);
                if (finalKeywords.Contains(groupingKeyword))
                {
                    ignoredItems.Add(item);
                    continue;
                }

                finalEntries.Add(new LDOCEEntry
                {
                    Keyword = groupingKeyword,
                    IsDuplicate = true,
                    Items = new List<LDOCEEntryItem> { item }
                });
                finalKeywords.Add(groupingKeyword);
            }

            Log("Skipped entries: {0}", _skippedCount);

            int noTranscriptionCount = finalEntries.SelectMany(x => x.Items).Count(x => string.IsNullOrEmpty(x.Transcription));
            Log("Total entries without transcription: {0}", noTranscriptionCount);

            Log(string.Join(Environment.NewLine, finalEntries.Where(x => x.IsDuplicate)
                .SelectMany(x => x.Items)
                .Select(x => string.Format("Added new alternative spelling entry '{0}' (original entry '{1}')", 
                    x.AlternativeSpelling, x.ItemKeyword))
                .OrderBy(x => x)));
            Log("\r\nTotally added {0} alternative spelling entries.\r\n", finalEntries.Count(x => x.IsDuplicate));

            Log(string.Join(Environment.NewLine, ignoredItems
                .Select(x => string.Format("Ignored alternative spelling entry '{0}' (original entry '{1}')",
                    x.AlternativeSpelling, x.ItemKeyword))
                .OrderBy(x => x)));
            Log("Totally ignored {0} alternative spelling entries.", ignoredItems.Count);

            Log("*** Ended parsing ***\r\n");
            File.AppendAllText(_logFile, _log.ToString());

            return finalEntries.ToArray();
        }

        private void ProcessEntry(LDOCEEntry entry, List<LDOCEEntry> entries)
        {
            if (entry == null || entry.Items.Count == 0)
            {
                _skippedCount++;
                Log("Skipped entry '{0}' because it's empty.", entry.Keyword);
                return;
            }

            var items = new List<LDOCEEntryItem>();
            LDOCEEntryItem lastItem = null;
            foreach (var item in entry.Items)
            {
                if (!item.HasAudio)
                {
                    if (string.IsNullOrEmpty(item.Transcription))
                    {
                        if (lastItem == null)
                        {
                            Log("Skipped entry without audio and transcription '{0}' {1}", entry.Keyword, item.ItemNumber);
                            continue;
                        }

                        MergeItems(lastItem, item);
                    }
                    else
                    {
                        var matchedItem = items.FirstOrDefault(x => x.Transcription == item.Transcription);
                        if (matchedItem == null)
                        {
                            items.Add(item);
                            lastItem = item;   
                        }
                        else
                        {
                            MergeItems(matchedItem, item);                         
                        }
                    }
                }
                else
                {
                    if (lastItem == null)
                    {
                        items.Add(item);
                        lastItem = item;
                    }
                    else
                    {
                        LDOCEEntryItem matchedItem = items.FirstOrDefault(x =>
                            x.SoundFileUK == item.SoundFileUK && x.SoundFileUS == item.SoundFileUS
                            && (x.Transcription == item.Transcription || string.IsNullOrEmpty(item.Transcription)));

                        if (matchedItem == null)
                        {
                            items.Add(item);
                            lastItem = item;
                        }
                        else
                        {
                            MergeItems(matchedItem, item);
                        }
                    }
                }
            }

            if (items.Count <= 0)
            {
                _skippedCount++;
                Log("Skipped entry '{0}' because its items are empty.", entry.Keyword);
                return;
            }

            entry.Items = items;
            entries.Add(entry);
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
        }

        private LDOCEEntryItem ParseNumberedItem(string text)
        {
            var reader = new TagReader(text);
            if (!reader.LoadTagContent("{{Roman}}[b]", "[/b]{{/Roman}}", false))
                throw new Exception("Entry number is missing!");

            string itemNumber = reader.Content;
            if (!reader.LoadRemainingText())
                throw new Exception("Entry number with empty content!");

            return ParseMainTag(itemNumber, reader.Content.TrimStart());
        }

        private LDOCEEntryItem ParseMainTag(string itemNumber, string text)
        {
            LDOCEEntryItem item = new LDOCEEntryItem { ItemNumber = itemNumber, RawData = text };
            var reader = new TagReader(text);
            const string tagEnd = "[/b][/c]";
            if (reader.LoadTagContent("[c blue][b]", new[] {
                new ClosingTagInfo(tagEnd, " "), 
                new ClosingTagInfo(tagEnd, "[/m]"),
                new ClosingTagInfo(tagEnd, "[sup]"),  
                new ClosingTagInfo(tagEnd, "[i][c]"),
                new ClosingTagInfo(tagEnd, "[b][c red]"),
                new ClosingTagInfo(tagEnd, "[i][c maroon]")}, true))
            {
                item.ItemTitle = PrepareTitle(reader.Content);
                item.ItemKeyword = PrepareKeyword(reader.Content);
            }

            item.Transcription = FindTranscription(reader);

            if (reader.LoadTagContent("[i][c] ", "[/c][/i]", false))
            {
                item.PartsOfSpeech = SplitPartsOfSpeech(Trim(reader.Content));
            }

            reader.ResetPosition();
            while (reader.LoadTagContent("[i][c maroon]", "[/c][/i]", false))
            {
                var note = Trim(reader.Content);
                if (note == "trademark")
                {
                    item.Notes = note;
                    break;
                }
            }

            reader.ResetPosition();
            if (reader.RewindToTag("[p]BrE[/p]"))
            {
                if (!reader.LoadTagContent("[s]", "[/s]", false))
                    throw new Exception("British pronunciation without file name!");

                item.SoundFileUK = Trim(reader.Content);
            }

            reader.ResetPosition();
            if (reader.RewindToTag("[p]AmE[/p]"))
            {
                if (!reader.LoadTagContent("[s]", "[/s]", false))
                    throw new Exception("American pronunciation without file name!");

                item.SoundFileUS = Trim(reader.Content);
            }

            ParseAlternativeSpelling(reader, item);
            return item;
        }

        private string FindTranscription(TagReader reader)
        {
            string transcription = null;
            if (reader.LoadTagContent(" /", new[] { 
                new ClosingTagInfo("/ "), new ClosingTagInfo("/, "), new ClosingTagInfo("/", "["), }, 
                true))
            {
                if (!reader.IsTagOpen("(", ")")
                    && reader.Content != null && !reader.Content.Contains("(") && !reader.Content.Contains(")"))
                {
                    transcription = Trim(PrepareTranscription(reader.Content));
                    if (transcription.Contains("[") || transcription.Contains("]"))
                        throw new Exception("Transcription contains unsupported tags inside!");
                }
            }

            return transcription;
        }

        private void ParseAlternativeSpelling(TagReader reader, LDOCEEntryItem item)
        {
            if (string.IsNullOrEmpty(item.ItemKeyword))
                return;

            reader.ResetPosition();
            if (reader.LoadTagContent("[b][c blue]", "[/c][/b]", false))
            {
                if (!reader.IsTagOpen("(", ")"))
                {
                    string wordRef = PrepareKeyword(reader.Content);
                    if (string.IsNullOrEmpty(wordRef))
                        return;

                    string alternativeTitle = PrepareTitle(reader.Content);
                    var transcription = FindTranscription(reader);
                    if (!string.IsNullOrEmpty(item.Transcription) && transcription == item.Transcription
                        && wordRef != item.ItemKeyword)
                    {
                        item.AlternativeSpelling = wordRef;
                        item.ItemTitle = MergeItemTitle(item.ItemTitle, alternativeTitle);
                    }
                }
            }
        }

        private string MergeItemTitle(string currentTitle, string alternativeTitle)
        {
            if (string.IsNullOrEmpty(alternativeTitle))
                return currentTitle;

            if (string.IsNullOrEmpty(currentTitle))
                return alternativeTitle;

            string concatenator = ",";
            if (currentTitle.Contains(",") || alternativeTitle.Contains(","))
            {
                concatenator = ";";
            }

            return string.Format("{0}{1} {2}", currentTitle, concatenator, alternativeTitle);
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

        private string PrepareTitle(string itemTitle)
        {
            if (string.IsNullOrEmpty(itemTitle))
                return itemTitle;

            var result = itemTitle.Replace("[b]", "").Replace("[/b]", "").Replace("‧", "").Replace("·", "").Replace("’", "'");
            if (result.Contains("[") || result.Contains("]"))
                throw new ArgumentException();

            return Trim(result);
        }

        private string PrepareKeyword(string itemTitle)
        {
            string result = PrepareTitle(itemTitle);
            if (string.IsNullOrEmpty(result))
                return result;

            return Trim(result.Replace("ˈ", "").Replace("ˌ", ""));
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
            
            return text
                .Replace("[i][c gray]", TranscriptionNoteOpenTag)
                .Replace("[/c][/i]", TranscriptionNoteCloseTag)
                .Replace("[i]", TranscriptionItalicOpenTag)
                .Replace("[/i]", TranscriptionItalicCloseTag)
                .Replace(",", string.Format("{0},{1}", TranscriptionSeparatorOpenTag, TranscriptionSeparatorCloseTag));
        }
    }
}
