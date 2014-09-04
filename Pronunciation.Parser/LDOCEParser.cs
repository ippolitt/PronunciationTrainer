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
                            RegisterEntry(entry, entries);
                        }
                        entry = new LDOCEEntry { Keyword = text, Items = new List<LDOCEEntryItem>() };
                    }

                    if (item != null)
                    {
                        if (string.IsNullOrEmpty(item.ItemText))
                        {
                            //Log("Entry without a text '{0}' {1}       {2}", entry.Keyword, item.ItemNumber, item.RawData);
                            continue;
                        }
                        
                        if (item.ItemText != entry.Keyword)
                        {
                            //Log("Entry with a different text '{0}' {1}", entry.Keyword, item.ItemNumber);
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
                RegisterEntry(entry, entries);
            }

            int noTranscriptionCount = entries.SelectMany(x => x.Items).Count(x => string.IsNullOrEmpty(x.Transcription));
            Log("Total entries without transcription: {0}", noTranscriptionCount);

            Log("*** Ended parsing ***\r\n");
            File.AppendAllText(_logFile, _log.ToString());

            return entries.ToArray();
        }

        private void RegisterEntry(LDOCEEntry entry, List<LDOCEEntry> entries)
        {
            if (entry == null || entry.Items.Count == 0)
                return;

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

            if (entry.Items.Count > 0)
            {
                entries.Add(entry);
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
                item.ItemStressedText = Trim(RemoveTitleTags(reader.Content));
                item.ItemText = Trim(RemoveWordStress(item.ItemStressedText));
            }

            if (reader.LoadTagContent(" /", new[] {new ClosingTagInfo( "/ "), new ClosingTagInfo("/", "[")}, true))
            {
                if (!reader.IsTagOpen("(", ")") 
                    && reader.Content != null && !reader.Content.Contains("(") && !reader.Content.Contains(")"))
                {
                    item.Transcription = Trim(PrepareTranscription(reader.Content));
                    if (item.Transcription.Contains("[") || item.Transcription.Contains("]"))
                        throw new Exception("Transcription contains unsupported tags inside!");
                }
            }

            if (reader.LoadTagContent("[i][c] ", "[/c][/i]", false))
            {
                item.PartsOfSpeech = SplitPartsOfSpeech(Trim(reader.Content));
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
            
            return item;
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

        private string RemoveTitleTags(string itemTitle)
        {
            if (string.IsNullOrEmpty(itemTitle))
                return itemTitle;

            var result = itemTitle.Replace("[b]", "").Replace("[/b]", "").Replace("‧", "").Replace("·", "");
            if (result.Contains("[") || result.Contains("]"))
                throw new ArgumentException();

            return result;
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
                .Replace("[/i]", TranscriptionItalicCloseTag);
        }
    }
}
