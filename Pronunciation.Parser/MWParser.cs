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

        private const string TheEnding = ", the";

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
  
                    if (text.StartsWith("["))
                    {
                        if (text.StartsWith("[b][c darkslategray]") || text.StartsWith("[s]"))
                        {
                            MWEntryItem item = ParseMainTag(text);
                            if (item != null)
                            {
                                item.ItemTitle = entry.Title;
                                entry.Items.Add(item);
                            }
                        }
                        else if (text.StartsWith(@"[com]\"))
                        {
                            // It means transcription (usually without audio):
                            // "distressed" -> [com]\\-ˈstrest\\[/com] [i][c][com]adjective[/com][/c][/i]
                            // "Abdelkader" -> [com]\\ˌab-ˌdel-ˈkä-dər\\[/com][i]or[/i] [b]Abd al-Qā·dir[/b] [s]bixabd02.wav[/s] [com]\\ˌab-dəl-\\[/com]   
                            continue;
                        }
                        else if (text.StartsWith("[com]"))
                        {
                            if (entry.Items.Count > 0)
                            {
                                ParseWordForms(entry.Items.Last(), text);
                            }
                        }
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
                        entry = new MWEntry 
                        { 
                            Keyword = PrepareKeyword(text),
                            Title = PrepareTitle(text),
                            Items = new List<MWEntryItem>() 
                        };
                    }
                }
            }

            if (entry != null)
            {
                RegisterEntry(entry, entries);
            }

            // Group entries by keyword (cut off ", the" ending)
            var finalEntries = new List<MWEntry>();
            foreach (var group in entries.GroupBy(x => PrepareGroupingKeyword(x.Keyword)))
            {
                var groupedItems = group.ToArray();
                MWEntry mainEntry;
                if (groupedItems.Length > 1)
                {
                    mainEntry = new MWEntry
                    {
                        Keyword = group.Key,
                        Title = group.Key,
                        Items = groupedItems.SelectMany(x => x.Items).ToList()
                    };
                }
                else
                {
                    mainEntry = groupedItems[0];
                    mainEntry.Keyword = group.Key;
                }

                finalEntries.Add(mainEntry);
            }

            Log("Skipped entries: {0}", _skippedCount);
            Log("Valid entries: {0}", finalEntries.Count);

            int noTranscriptionCount = finalEntries.SelectMany(x => x.Items).Count(x => string.IsNullOrEmpty(x.Transcription));
            Log("Total entries without transcription: {0}", noTranscriptionCount);

            Log("*** Ended parsing ***\r\n");
            File.AppendAllText(_logFile, _log.ToString());

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

            item.SoundFiles = CollectSoundFiles(reader);

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

        // [com]([b]abler[/b] [s]able0002.wav[/s] \\-b(ə-)lər\\ ; [b]ablest[/b] [s]able0003.wav[/s] \\-b(ə-)ləst\\)[/com]
        // [com]([i]plural[/i] [b]ab·a·tis[/b] [s]abatis03.wav[/s] \\ˈa-bə-ˌtēz \\ ; [i]or[/i] [b]ab·a·tis·es[/b] [s]abatis04.wav[/s] \\ˈa-bə-tə-səz\\)[/com]
        private void ParseWordForms(MWEntryItem item, string text)
        {
            var reader = new TagReader(text);
            if (!reader.LoadTagContent("[com](", ")[/com]", true))
                throw new ArgumentException();

            bool isPluralForm = Trim(reader.Content).StartsWith("[i]plural[/i]");
            string[] forms = reader.Content.Split(new [] {" ; "}, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => Trim(x)).Where(x => !string.IsNullOrEmpty(x)).ToArray();
            if (forms.Length == 0)
                throw new ArgumentException();

            if (item.WordForms != null)
                throw new ArgumentException();

            foreach (var formText in forms)
            {
                var form = ParseWordForm(formText);
                if (form == null)
                    continue;

                if ((form.SoundFiles == null || form.SoundFiles.Count == 0) && string.IsNullOrEmpty(form.Transcription))
                    continue;

                form.IsPluralForm = isPluralForm;
                if (item.WordForms == null)
                {
                    item.WordForms = new List<MWWordForm>();
                }
                item.WordForms.Add(form);
            }
        }
    
        private MWWordForm ParseWordForm(string text)
        {
            var reader = new TagReader(text, new[] { "[", "]", @"\\" });
            string formNames = null;
            string transcription = null;
            List<string> soundFiles = new List<string>();
            bool isBeenForm = text.StartsWith("[i]plural[/i] [b]were[/b]");
            bool isTranscriptionRef = false;
            // We are searching for the pattern: (note -> name)*N -> sound(s) -> transcription
            while (true)
            {
                reader.LoadTagContent("[i]", "[/i]", false, true);
                if (!reader.LoadTagContent("[b]", "[/b]", false, true))
                    break;

                string name = Trim(reader.Content);
                if (isBeenForm && name == "were")
                    continue;

                if (!string.IsNullOrEmpty(formNames))
                {
                    formNames += ", ";
                }
                formNames += PrepareFormName(name);

                while (reader.LoadTagContent("[s]", "[/s]", false, true))
                {
                    RegisterSoundFile(reader.Content, soundFiles);
                }

                // Special case for "Hasid"
                if (reader.LoadTagContent("[i]", "[/i]", false, true))
                {
                    while (reader.LoadTagContent("[s]", "[/s]", false, true))
                    {
                        RegisterSoundFile(reader.Content, soundFiles);
                    }
                }

                if (reader.LoadTagContent(@"\\", @"\\", true, true))
                {
                    if (!string.IsNullOrEmpty(transcription))
                        throw new ArgumentException();

                    // Special case for "neckerchief"
                    isTranscriptionRef = Trim(reader.Content).StartsWith("[i]see[/i]");
                    if (!isTranscriptionRef)
                    {
                        transcription = PrepareTranscription(Trim(reader.Content));
                    }
                }
            }

            if (string.IsNullOrEmpty(formNames))
                return null;

            var form = new MWWordForm 
            { 
                FormName = formNames, 
                Transcription = transcription,
                SoundFiles = soundFiles.Count > 0 ? soundFiles : null
            };
            
            // Ensure that we haven't skipped any sounds
            reader.ResetPosition();
            var allSounds = CollectSoundFiles(reader);
            if ((allSounds == null ? 0 : allSounds.Count) != (form.SoundFiles == null ? 0 : form.SoundFiles.Count))
                throw new ArgumentException();

            // Ensure that we haven't skipped a transcription
            if (!isTranscriptionRef)
            {
                transcription = null;
                reader.ResetPosition();
                while (reader.LoadTagContent(@"\\", @"\\", true))
                {
                    if (!string.IsNullOrEmpty(transcription))
                        throw new ArgumentException();

                    transcription = PrepareTranscription(Trim(reader.Content));
                }
                if (form.Transcription != transcription)
                    throw new ArgumentException();
            }

            return form;
        }

        private List<string> CollectSoundFiles(TagReader reader)
        {
            var soundFiles = new List<string>();
            while (reader.LoadTagContent("[s]", "[/s]", false))
            {
                RegisterSoundFile(reader.Content, soundFiles);
            }

            return soundFiles.Count == 0 ? null : soundFiles;
        }

        private void RegisterSoundFile(string content, List<string> soundFiles)
        {
            string soundFile = Trim(content);
            if (string.IsNullOrEmpty(soundFile) || !soundFile.EndsWith(".wav"))
                return;

            // It's important to preserve the order of the sounds (they often match transcriptions order)
            // so we use List instead of hashset
            if (!soundFiles.Contains(soundFile))
            {
                soundFiles.Add(soundFile);
            }
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

                MWEntryItem matchedItem = null;
                if (!item.HasSounds)
                {
                    if (string.IsNullOrEmpty(item.Transcription))
                    {
                        if (lastItem == null)
                            continue;

                        matchedItem = lastItem;
                    }
                    else
                    {
                        matchedItem = items.FirstOrDefault(x => x.Transcription == item.Transcription);
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(item.Transcription))
                    {
                        matchedItem = items.FirstOrDefault(x => SoundsEqual(x.SoundFiles, item.SoundFiles));
                    }
                    else
                    {
                        matchedItem = items.FirstOrDefault(x => x.Transcription == item.Transcription);
                    }
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

            // Used in word forms
            if (text == "[i]same[/i]")
                return null;

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
            var title = PrepareTitle(text);
            if (!string.IsNullOrEmpty(title))
            {
                title = title.Replace("<sub>", "").Replace("</sub>", "");
            }

            return title;
        }

        private string PrepareTitle(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return text.Replace("{·}", "")
                .Replace(@"\", "")
                .Replace("{[sub]}", "<sub>")
                .Replace("{[/sub]}", "</sub>");
        }

        private string PrepareFormName(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            text = text.Replace("·", "");
            if (text.Contains("[") || text.Contains(@"\") || text.Contains("|"))
                throw new ArgumentException();

            return text;
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

            if (source.SoundFiles != null && source.SoundFiles.Count > 0)
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

            if (source.WordForms != null && source.WordForms.Count > 0)
            {
                if (target.WordForms == null)
                {
                    target.WordForms = source.WordForms;
                }
                else
                {
                    target.WordForms.AddRange(source.WordForms);
                }
            }
        }

        private bool SoundsEqual(ICollection<string> source, ICollection<string> target)
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

        private string PrepareGroupingKeyword(string keyword)
        {
            if (!keyword.ToLower().EndsWith(TheEnding))
                return keyword;

            return Trim(keyword.Substring(0, keyword.Length - TheEnding.Length));
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
