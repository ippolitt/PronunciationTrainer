using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pronunciation.Parser
{
    class MWParser
    {
        private readonly string _logFile;
        private readonly StringBuilder _log;
        private readonly PartOfSpeechResolver _partOfSpeechResolver;
        private int _skippedCount = 0;

        public const string TranscriptionRefOpenTag = "<ref>";
        public const string TranscriptionRefCloseTag = "</ref>";
        public const string TranscriptionNoteOpenTag = "<note>";
        public const string TranscriptionNoteCloseTag = "</note>";
        public const string TranscriptionItalicOpenTag = "<i>";
        public const string TranscriptionItalicCloseTag = "</i>";
        public const string TranscriptionUnderlinedOpenTag = "<u>";
        public const string TranscriptionUnderlinedCloseTag = "</u>";
        public const string TranscriptionSeparatorOpenTag = "<sp>";
        public const string TranscriptionSeparatorCloseTag = "</sp>";

        public MWParser(string logFile)
        {
            _logFile = logFile;
            _log = new StringBuilder();
            _partOfSpeechResolver = new PartOfSpeechResolver(InitPartsOfSpeech());

            File.WriteAllText(logFile, DateTime.Now.ToString());
        }

        public MWEntry[] Parse(string sourceFile)
        {
            Log("*** Started parsing ***\r\n");
            //var lst = new List<string>();
            //var lst2 = new List<string>();
            var entries = new List<MWEntry>();
            MWEntry entry = null;
            using (var reader = new StreamReader(sourceFile))
            {
                while (!reader.EndOfStream)
                {
                    var text = reader.ReadLine().Trim();
                    if (string.IsNullOrEmpty(text))
                        continue;

                    if (text.StartsWith("#"))
                        continue;

                    if (text.StartsWith("{{"))
                        break;

                    MWEntryItem item = null;
                    if (text.StartsWith("["))
                    {
                        if (text.StartsWith("[b][c darkslategray]") || text.StartsWith("[s]"))
                        {
                            item = ParseMainTag(text);
                        }
                        //else if (text.Contains(".wav"))
                        //{
                        //    if (text.StartsWith("[com]") || text.StartsWith("[m1]•")
                        //        // || text.StartsWith("[m2]")
                        //        //|| text.StartsWith("[i]also") || text.StartsWith("[i]or")
                        //        )
                        //    {
                        //        lst.Add(string.Format("{0}\r\n\t{1}", entry == null ? null : entry.Keyword, text));
                        //        lst2.Add(text);
                        //    }
                        //}
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (entry != null)
                        {
                            RegisterEntry(entry, entries);
                        }
                        entry = new MWEntry { Keyword = PrepareKeyword(text), Items = new List<MWEntryItem>() };
                    }

                    if (item != null)
                    {
                        entry.Items.Add(item);  
                    }
                }
            }

            if (entry != null)
            {
                RegisterEntry(entry, entries);
            }

            Log("Skipped entries: {0}", _skippedCount);
            Log("Valid entries: {0}", entries.Count);

            int noTranscriptionCount = entries.SelectMany(x => x.Items).Count(x => string.IsNullOrEmpty(x.Transcription));
            Log("Total entries without transcription: {0}", noTranscriptionCount);

            Log("*** Ended parsing ***\r\n");
            File.AppendAllText(_logFile, _log.ToString());
            //File.WriteAllText(@"D:\test.txt", string.Join(Environment.NewLine, lst.OrderBy(x => x)));
            //File.WriteAllText(@"D:\test2.txt", string.Join(Environment.NewLine, lst2.GroupBy(x => x.Substring(0, 8))
            //    .Select(x => x.Key + " " + x.Count()).OrderBy(x => x)));
            return entries.ToArray();
        }

        // [s] .wav or .jpg [/s]
        // [b][c darkslategray]I. [/c][/b]
        // [i][c][com]abbreviation[/com][/c][/i]
        // [com]\\ˈärd-ˌvärk\\[/com]
        // [com]\\ˈä, [i]often prolonged and/or followed by[/i] ə\\[/com]
        // [b][c darkslategray]I. [/c][/b][s]proces02.wav[/s] [s]proces01.wav[/s] [com]\\ˈprä-ˌses, ˈprō-, -səs\\[/com] [i][c][com]noun[/com][/c][/i]
        // [b][c darkslategray]II. [/c][/b][i][c][com]transitive verb[/com][/c][/i]
        private MWEntryItem ParseMainTag(string text)
        {
            var item = new MWEntryItem { RawData = text };
            var reader = new TagReader(text);
            if (reader.LoadTagContent("[b][c darkslategray]", "[/c][/b]", false))
            {
                if (!string.IsNullOrEmpty(reader.Content))
                {
                    item.ItemNumber = Trim(reader.Content).Replace(".", "");
                }
            }

            while (reader.LoadTagContent("[s]", "[/s]", false))
            {
                string soundFile = Trim(reader.Content);
                if (string.IsNullOrEmpty(soundFile) || !soundFile.EndsWith(".wav"))
                    continue;

                if (item.SoundFiles == null)
                {
                    // It's important to preserve the order of the sounds (they often match transcriptions order)
                    // so we use List instead of hashset
                    item.SoundFiles = new List<string>();
                }
                if (!item.SoundFiles.Contains(soundFile))
                {
                    item.SoundFiles.Add(soundFile);
                }
            }

            if (reader.LoadTagContent(@"[com]\\", @"\\[/com]", true))
            {
                item.Transcription = Trim(reader.Content);
            }

            if (reader.LoadTagContent("[i][c][com]", "[/com][/c][/i]", true))
            {
                item.PartsOfSpeech = PrepareParstOfSpeech(Trim(reader.Content));
            }

            return item;
        }

        private void RegisterEntry(MWEntry entry, List<MWEntry> entries)
        {
            if (entry.Items.Count == 0)
            {
                _skippedCount++;
                Log("Skipped entry '{0}' because it's empty.", entry.Keyword);
                return;
            }

            // same as
            var items = new List<MWEntryItem>();
            MWEntryItem lastItem = null;
            foreach (var item in entry.Items)
            {
                if (!ResolveTranscriptionReference(item, entry.Items))
                {
                    item.Transcription = PrepareTranscription(item.Transcription);
                }

                if (!item.HasSounds)
                {
                    if (lastItem == null)
                        continue;

                    if (string.IsNullOrEmpty(item.Transcription))
                    {
                        MergeItems(lastItem, item);
                    }
                    else
                    {
                        var matchedItem = items.FirstOrDefault(x => x.Transcription == item.Transcription);
                        if (matchedItem != null)
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
                        continue;
                    }

                    MWEntryItem matchedItem;
                    if (string.IsNullOrEmpty(item.Transcription))
                    {
                        matchedItem = items.FirstOrDefault(x => SoundsEqual(x.SoundFiles, item.SoundFiles));
                    }
                    else
                    {
                        matchedItem = items.FirstOrDefault(x => x.Transcription == item.Transcription);
                    }

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

            if (items.Count <= 0)
            {
                _skippedCount++;
                Log("Skipped entry '{0}' because its items are empty.", entry.Keyword);
                return;
            }

            entry.Items = items;
            entries.Add(entry);
        }

        // [i]same as[/i] [sup]2[/sup]
        // [i]same as [sup]1[/sup][/i]
        // [i]same as 1[/i]
        // [i]same as[/i] [ref]confidant[/ref]
        private bool ResolveTranscriptionReference(MWEntryItem item, List<MWEntryItem> items)
        {
            if (string.IsNullOrEmpty(item.Transcription))
                return false;

            if (!item.Transcription.StartsWith("[i]same as"))
                return false;

            string entryNumber = null;
            var reader = new TagReader(item.Transcription);
            if (reader.RewindToTag("[i]same as[/i]"))
            {
                if (reader.LoadTagContent("[sup]", "[/sup]", false))
                {
                    entryNumber = Trim(reader.Content);
                }
            }
            else if (reader.RewindToTag("[i]same as 1[/i]"))
            {
                entryNumber = "1";
            }
            else if (reader.RewindToTag("[i]same as "))
            {
                if (reader.LoadTagContent("[sup]", "[/sup]", false))
                {
                    entryNumber = Trim(reader.Content);
                }
            }

            if (string.IsNullOrEmpty(entryNumber))
                return false;

            item.Transcription = items[int.Parse(entryNumber) - 1].Transcription;
            return true;
        }

        private string PrepareTranscription(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            string result = ReplaceTranscriptionDashes(text);
            result = result
                .Replace("[ref]", TranscriptionRefOpenTag)
                .Replace("[/ref]", TranscriptionRefCloseTag)
                .Replace("[i]", TranscriptionNoteOpenTag)
                .Replace("[/i]", TranscriptionNoteCloseTag)
                .Replace("[u]", TranscriptionUnderlinedOpenTag)
                .Replace("[/u]", TranscriptionUnderlinedCloseTag)
                .Replace("[sup]", "<sup>")
                .Replace("[/sup]", "</sup>")
                .Replace(",", string.Format("{0},{1}", TranscriptionSeparatorOpenTag, TranscriptionSeparatorCloseTag))
                .Replace(";", string.Format("{0};{1}", TranscriptionSeparatorOpenTag, TranscriptionSeparatorCloseTag))
                .Replace("(", TranscriptionItalicOpenTag)
                .Replace(")", TranscriptionItalicCloseTag)
                .Replace(string.Format("{0}ˌ{1}", TranscriptionItalicOpenTag, TranscriptionItalicCloseTag), 
                    "<sub>(</sub>ˌ<sub>)</sub>")
                .Replace(string.Format("{0}ˈ{1}", TranscriptionItalicOpenTag, TranscriptionItalicCloseTag),
                    "<sup>(</sup>ˈ<sup>)</sup>");

            if (result.Contains("[") || result.Contains("]"))
                throw new ArgumentException();

            return result;
        }

        private string ReplaceTranscriptionDashes(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var bld = new StringBuilder();
            int i = 0;
            int maxIndex = text.Length - 1;
            foreach (char ch in text)
            {
                if (ch == '-' && i > 0 && i < maxIndex 
                    && IsTranscriptionSymbol(text[i - 1]) && IsTranscriptionSymbol(text[i + 1]))
                {
                    bld.Append(' ');
                }
                else
                {
                    bld.Append(ch);
                }

                i++;
            }

            return bld.ToString();
        }

        private bool IsTranscriptionSymbol(char ch)
        {
            return ch != ' ' && ch != ',' && ch != ';' && ch != '[' && ch != ']';
        }

        private string PrepareKeyword(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return text.Replace("{·}", "")
                .Replace(@"\", "")
                .Replace("{[sub]}", "<sub>")
                .Replace("{[/sub]}", "</sub>");
        }

        private List<string> PrepareParstOfSpeech(string partsOfSpeech)
        {
            if (string.IsNullOrEmpty(partsOfSpeech))
                return null;

            var parts = new List<string>();
            foreach (var part in partsOfSpeech
                .Split(new [] { ",", " or "}, StringSplitOptions.RemoveEmptyEntries)
                .Where(x => !string.IsNullOrEmpty(x) && !x.Contains("[ref]"))
                .Select(x => _partOfSpeechResolver.FindPartOfSpeech(Trim(x)))
                .Where(x => !string.IsNullOrEmpty(x)))
            {
                parts.Add(part);
            }

            return parts.Count == 0 ? null : parts;
        }

        private void MergeItems(MWEntryItem target, MWEntryItem source)
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

            if (source.SoundFiles != null)
            {
                if (target.SoundFiles == null)
                {
                    target.SoundFiles = source.SoundFiles;
                }
                else
                {
                    target.SoundFiles.AddRange(source.SoundFiles.Where(x => !target.SoundFiles.Contains(x)));
                }
            }
        }

        private bool SoundsEqual(List<string> source, List<string> target)
        {
            if (source != null && target != null)
            {
                if (source.Count != target.Count)
                    return false;

                foreach(var item in source)
                {
                    if (!target.Contains(item))
                        return false;
                }

                return true;
            }
            else
            {
                return (source == null && target == null);
            }
        }

        private string Trim(string text)
        {
            return string.IsNullOrEmpty(text) ? text : text.Trim();
        }

        private void Log(string format, params object[] args)
        {
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

        private static string[] InitPartsOfSpeech()
        {
            return new string[] 
            { 
                "prefix",
                "suffix",
                "combining form",
                "abbreviation",
                "adjective",
                "adverb",
                "biographical name",
                "certification mark",
                "collective mark",
                "conjunction",
                "definite article",
                "foreign term",
                "geographical name",
                "indefinite article",
                "interjection",
                "noun",
                "phrasal",
                "preposition",
                "pronoun",
                "service mark",
                "symbol",
                "trademark",
                "verb",
                "verbal auxiliary"
            };
        }
    }
}
