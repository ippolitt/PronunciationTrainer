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
                    else
                    {
                        if (entry != null)
                        {
                            ProcessEntry(entry);
                            if (entry.Items.Count > 0)
                            {
                                entries.Add(entry);
                            }
                        }
                        entry = new LDOCEEntry { Keyword = RemoveExtraText(text), Items = new List<LDOCEEntryItem>() };
                    }

                    if (item != null)
                    {
                        if (string.IsNullOrEmpty(item.ItemText))
                        {
                            //Log("Entry without a text '{0}' {1}       {2}", entry.Keyword, item.ItemNumber, item.RawData);
                            continue;
                        }

                        if (item.ItemText != entry.Keyword && item.AlternativeSpelling != entry.Keyword)
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
                ProcessEntry(entry);
                if (entry.Items.Count > 0)
                {
                    entries.Add(entry);
                }
            }

            // Group entries by keyword
            var finalEntries = new List<LDOCEEntry>();
            foreach (var group in entries.GroupBy(x => x.Keyword))
            {
                var mainEntry = group.FirstOrDefault(x => !x.IsDuplicateEntry);
                if (mainEntry == null)
                {
                    mainEntry = group.FirstOrDefault();
                }

                finalEntries.Add(mainEntry);
            }

            int noTranscriptionCount = finalEntries.SelectMany(x => x.Items).Count(x => string.IsNullOrEmpty(x.Transcription));
            Log("Total entries without transcription: {0}", noTranscriptionCount);

            Log(string.Join(Environment.NewLine, finalEntries.Where(x => x.IsDuplicateEntry)
                .SelectMany(x => x.Items)
                .Select(x => string.Format("Extracted entry '{0}' -> '{1}'", x.AlternativeSpelling, x.ItemText))
                .OrderBy(x => x)));
            int extractedCount = finalEntries.Count(x => x.IsDuplicateEntry);
            Log("Total entries extracted: {0}", extractedCount);

            Log("*** Ended parsing ***\r\n");
            File.AppendAllText(_logFile, _log.ToString());

            return finalEntries.ToArray();
        }

        int pp = 0;
        private void ProcessEntry(LDOCEEntry entry)
        {
            if (entry == null || entry.Items.Count == 0)
                return;

            if (entry.Items.Any(x => x.ItemText != entry.Keyword))
            {
                if (entry.Items.Count > 1)
                {
                    pp += entry.Items.RemoveAll(x => x.ItemText != entry.Keyword);
                }
                else
                {
                    entry.IsDuplicateEntry = true;
                }
            }

            var groupsWithTranscription = entry.Items.Where(x => !string.IsNullOrEmpty(x.Transcription))
                .GroupBy(x => x.Transcription).ToArray();
            if (groupsWithTranscription.Length > 0)
            {
                var itemsWithoutTranscription = entry.Items.Where(x => string.IsNullOrEmpty(x.Transcription)).ToArray();
                foreach (var group in groupsWithTranscription)
                {
                    var groupItems = group.ToArray();
                    var mainItem = groupItems[0];

                    // We merge parts of speech only if the main entry has an explicit part of speech
                    bool collectPartsOfSpeech = (mainItem.PartsOfSpeech != null && mainItem.PartsOfSpeech.Length > 0);
                    List<string> partsOfSpeech = collectPartsOfSpeech ? new List<string>() : null;
                    if (groupItems.Length > 1)
                    {
                        if (collectPartsOfSpeech)
                        {
                            partsOfSpeech.AddRange(groupItems.Where((x, i) => i > 0 && x.PartsOfSpeech != null)
                                .SelectMany(x => x.PartsOfSpeech));
                        }
                        foreach (var item in groupItems.Where((x, i) => i > 0).ToArray())
                        {
                            //Log("Skipped duplicate entry '{0}' {1}", entry.Keyword, item.ItemNumber);
                            entry.Items.Remove(item);
                        }
                    }

                    if (collectPartsOfSpeech && itemsWithoutTranscription.Length > 0)
                    {
                        // If there's only one item with transcription then "assign" all items without a transcription to it
                        if (groupsWithTranscription.Length == 1)
                        {
                            partsOfSpeech.AddRange(itemsWithoutTranscription.Where(x => x.PartsOfSpeech != null)
                                .SelectMany(x => x.PartsOfSpeech));
                        }
                        else if (mainItem.HasAudio)
                        {
                            // Trying to match items without transcription by UK & US pronunciations
                            partsOfSpeech.AddRange(itemsWithoutTranscription.Where(x => x.PartsOfSpeech != null
                                    && x.SoundFileUK == mainItem.SoundFileUK && x.SoundFileUS == mainItem.SoundFileUS)
                                .SelectMany(x => x.PartsOfSpeech));
                        }
                    }

                    if (collectPartsOfSpeech && partsOfSpeech.Count > 0)
                    {
                        partsOfSpeech.AddRange(mainItem.PartsOfSpeech);
                        mainItem.PartsOfSpeech = partsOfSpeech.Distinct().ToArray();
                    }

                    if (!mainItem.HasAudio)
                    {
                        Log("Entry without an audio '{0}' {1}", entry.Keyword, mainItem.ItemNumber);
                    }
                }

                foreach (var item in itemsWithoutTranscription)
                {
                    entry.Items.Remove(item);
                }
            }
            else
            {
                var mainItem = entry.Items[0];
                if (entry.Items.Count > 1 && mainItem.PartsOfSpeech != null && mainItem.PartsOfSpeech.Length > 0)
                {
                    mainItem.PartsOfSpeech = entry.Items.Where(x => x.PartsOfSpeech != null)
                        .SelectMany(x => x.PartsOfSpeech).Distinct().ToArray();
                }

                Log("Entry without a transcription '{0}' {1}", entry.Keyword, mainItem.ItemNumber);
                if (!mainItem.HasAudio)
                {
                    Log("Entry without an audio '{0}' {1}", entry.Keyword, mainItem.ItemNumber);
                }

                foreach (var item in entry.Items.Where((x, i) => i > 0).ToArray())
                {
                    //Log("Skipped duplicate entry '{0}' {1}", entry.Keyword, item.ItemNumber);
                    entry.Items.Remove(item);
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
                item.ItemStressedText = Trim(PrepareTitle(reader.Content));
                item.ItemText = Trim(RemoveWordStress(item.ItemStressedText));
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

            AssignAlternativeSpelling(reader, item);
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

        private void AssignAlternativeSpelling(TagReader reader, LDOCEEntryItem item)
        {
            if (string.IsNullOrEmpty(item.ItemText))
                return;

            reader.ResetPosition();
            if (reader.LoadTagContent("[b][c blue]", "[/c][/b]", false))
            {
                if (!reader.IsTagOpen("(", ")"))
                {
                    string stressText = PrepareTitle(reader.Content);
                    string wordText = Trim(RemoveWordStress(stressText));
                    if (string.IsNullOrEmpty(wordText))
                        return;

                    var transcription = FindTranscription(reader);
                    if (!string.IsNullOrEmpty(item.Transcription) && transcription == item.Transcription
                        && wordText != item.ItemText)
                    {
                        item.AlternativeSpelling = wordText;
                        item.AlternativeStressedText = stressText;
                    }
                }
            }
        }

        private string Trim(string text)
        {
            return string.IsNullOrEmpty(text) ? text : text.Trim();
        }

        private string[] SplitPartsOfSpeech(string partsOfSpeech)
        {
            if (string.IsNullOrEmpty(partsOfSpeech))
                return null;

            return partsOfSpeech.Split(',').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();
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

            var result = itemTitle.Replace("[b]", "").Replace("[/b]", "").Replace("‧", "").Replace("·", "");
            if (result.Contains("[") || result.Contains("]"))
                throw new ArgumentException();

            result = RemoveExtraText(result);
            return result;
        }

        private string RemoveExtraText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            if (text.ToLower().EndsWith(TheEnding))
            {
                text = text.Substring(0, text.Length - TheEnding.Length).Trim();
            }

            return text;
        }

        private string RemoveWordStress(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return text.Replace("ˈ", "").Replace("ˌ", "");
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
