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

        private const string VariantStartOr = "[i]or[/i]";
        private const string VariantStartAlso = "[i]also[/i]";
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
            int recordNumber = 0;
            using (var reader = new StreamReader(sourceFile))
            {
                while (!reader.EndOfStream)
                {
                    var text = reader.ReadLine().Trim();
                    if (string.IsNullOrEmpty(text))
                        continue;

                    if (text.StartsWithOrdinal("#"))
                        continue;

                    if (text.StartsWithOrdinal("{{"))
                        break;

                    if (text.StartsWithOrdinal("["))
                    {
                        recordNumber++;
                        if (text.StartsWithOrdinal("[b][c darkslategray]") || text.StartsWithOrdinal("[s]") ||
                            (recordNumber == 1 && (text.StartsWithOrdinal("[i][c][com]") || text.StartsWithOrdinal(@"[com]\"))))
                        {
                            MWEntryItem item = ParseMainTag(text);
                            if (item != null)
                            {
                                item.ItemTitle = entry.Title.Clone();
                                entry.Items.Add(item);
                            }
                        }
                        else if (text.StartsWithOrdinal(VariantStartOr) || text.StartsWithOrdinal(VariantStartAlso)
                            || (recordNumber > 1 && text.StartsWithOrdinal(@"[com]\")))
                        {
                            var lastItem = entry.Items.LastOrDefault();
                            var variants = ParseVariant(entry, lastItem, text);
                            if (variants != null)
                            {
                                foreach (var variant in variants)
                                {
                                    RegisterEntry(variant, entries);
                                }
                                if (lastItem != null)
                                {
                                    lastItem.Variants = variants;
                                }
                            }
                        }
                        else if (text.StartsWithOrdinal("[com]"))
                        {
                            if (entry.Items.Count > 0)
                            {
                                var lastItem = entry.Items.Last();
                                ParseWordForms(lastItem, text);

                                if (lastItem.Variants != null && lastItem.WordForms != null && lastItem.WordForms.Count > 0)
                                {
                                    foreach (var variantItem in lastItem.Variants.SelectMany(x => x.Items))
                                    {
                                        variantItem.WordForms = lastItem.WordForms;
                                    }
                                }
                            }
                        }
                        else if (text.StartsWithOrdinal("[m1]•"))
                        {
                            List<MWEntryItem> derivedItems;
                            List<MWEntry> derivedEntries = ParseDerivedWords(entry, text, out derivedItems);
                            if (derivedEntries != null)
                            {
                                foreach (var derivedEntry in derivedEntries)
                                {
                                    RegisterEntry(derivedEntry, entries);
                                }
                            }
                            if (derivedItems != null && derivedItems.Count > 0)
                            {
                                entry.Items.AddRange(derivedItems);
                            }
                        }
                        else if (text.StartsWithOrdinal("[m1]"))
                        {
                            List<MWEntry> extraEntries = ParseExtraWords(text);
                            if (extraEntries != null)
                            {
                                foreach (var extraEntry in extraEntries)
                                {
                                    RegisterEntry(extraEntry, entries);
                                }
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
                        recordNumber = 0;
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
                MWEntry mainEntry = MergeEntries(group);
                mainEntry.Keyword = group.Key;

                finalEntries.Add(mainEntry);
            }

            Log("\r\nTOTALS:");
            Log("Final entries: {0}", finalEntries.Count);
            Log("Skipped entries: {0}", _skippedCount);
            Log("Items without sound: {0}", finalEntries.SelectMany(x => x.Items)
                .Count(x => x.SoundFiles == null || x.SoundFiles.Count == 0));
            Log("Items without transcription: {0}", finalEntries.SelectMany(x => x.Items)
                .Count(x => string.IsNullOrEmpty(x.Transcription)));

            Log("*** Ended parsing ***\r\n");
            File.AppendAllText(_logFile, _log.ToString());

            return finalEntries.ToArray();
        }

        private MWEntry MergeEntries(IEnumerable<MWEntry> groupedEntries)
        {
            var entries = groupedEntries.ToArray();
            if (entries.Length == 1)
                return entries[0];

            MWEntry result;
            var originalEntries = entries.Where(x => !x.IsDerived && !x.IsVariant).ToArray();
            if (originalEntries.Length == 0)
            {
                var variants = entries.Where(x => !x.IsDerived).ToArray();
                if (variants.Length == 0)
                {
                    result = entries.First();
                }
                else
                {
                    result = variants[0];
                    if (variants.Length > 1)
                    {
                        foreach (var item in variants.Where((x, i) => i > 0).SelectMany(x => x.Items))
                        {
                            var matchedItem = result.Items.FirstOrDefault(x => SoundsEqual(x.SoundFiles, item.SoundFiles)
                                && (string.IsNullOrEmpty(x.Transcription) || string.IsNullOrEmpty(item.Transcription)
                                    || x.Transcription == item.Transcription)
                                && (item.WordForms == null || item.WordForms.Count == 0));
                            if (matchedItem == null)
                            {
                                result.Items.Add(item);
                            }
                            else
                            {
                                MergeTitles(matchedItem, item, true);

                                if (string.IsNullOrEmpty(matchedItem.Transcription))
                                {
                                    matchedItem.Transcription = item.Transcription;
                                }

                                if (matchedItem.PartsOfSpeech == null || matchedItem.PartsOfSpeech.Count == 0)
                                {
                                    matchedItem.PartsOfSpeech = item.PartsOfSpeech;
                                }
                                else if (item.PartsOfSpeech != null && item.PartsOfSpeech.Count > 0)
                                {
                                    matchedItem.PartsOfSpeech.AddRange(item.PartsOfSpeech
                                        .Where(x => !matchedItem.PartsOfSpeech.Contains(x)));
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                result = originalEntries[0];
                if (originalEntries.Length > 1)
                {
                    result.Items = originalEntries.SelectMany(x => x.Items).ToList();
                }
            }

            return result;
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
                item.IsRawTranscription = true;
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

            bool isPluralForm = Trim(reader.Content).StartsWithOrdinal("[i]plural[/i]");
            string[] forms = reader.Content.Split(new [] {" ; "}, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => Trim(x)).Where(x => !string.IsNullOrEmpty(x)).ToArray();
            if (forms.Length == 0)
                throw new ArgumentException();

            if (item.WordForms != null)
                throw new ArgumentException();

            foreach (var formText in forms)
            {
                var form = ParseWordForm(formText, isPluralForm);
                if (form == null)
                    continue;

                if ((form.SoundFiles == null || form.SoundFiles.Count == 0) && string.IsNullOrEmpty(form.Transcription))
                    continue;

                if (item.WordForms == null)
                {
                    item.WordForms = new List<MWWordForm>();
                }
                else if (item.WordForms.Any(x => x.IsPluralForm == form.IsPluralForm && x.Title.IsEqual(form.Title)
                    && SoundsEqual(x.SoundFiles, form.SoundFiles)))
                {
                    // Special case for "cleaved" which appears two times in "cleave"
                    continue;
                }
                item.WordForms.Add(form);
            }
        }

        private MWWordForm ParseWordForm(string text, bool isPluralForm)
        {
            var reader = new TagReader(text, new[] { "[", "]", @"\\" });
            DisplayName title = null;
            string transcription = null;
            List<string> soundFiles = new List<string>();
            bool isBeenForm = text.StartsWithOrdinal("[i]plural[/i] [b]were[/b]");
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

                string formName = PrepareFormName(name);

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
                    isTranscriptionRef = Trim(reader.Content).StartsWithOrdinal("[i]see[/i]");
                    if (!isTranscriptionRef)
                    {
                        transcription = PrepareTranscription(Trim(reader.Content));
                    }
                }

                if (!string.IsNullOrEmpty(formName) && (soundFiles.Count > 0 || !string.IsNullOrEmpty(transcription)))
                {
                    if (title == null)
                    {
                        title = new DisplayName(formName);
                    }
                    else if (formName != "cleaving")
                    {
                        title.Append(formName);
                    }
                }
            }

            if (title == null)
                return null;

            var form = new MWWordForm 
            { 
                IsPluralForm = isPluralForm,
                Title = title, 
                Transcription = transcription,
                SoundFiles = soundFiles.Count > 0 ? soundFiles : null
            };

            // Ensure that we haven't skipped any sounds or transcription
            ValidateSoundFiles(reader, form.SoundFiles);
            if (!isTranscriptionRef)
            {
                ValidateTranscription(reader, form.Transcription, true);
            }

            return form;
        }

        // absinthe
        // [i]also[/i] [b]ab·sinth[/b] [s]absint01.wav[/s] [com]\\ˈab-(ˌ)sin(t)th\\[/com]
        // acknowledgment
        // [i]or[/i] [b]ac·knowl·edge·ment[/b] [s]acknow05.wav[/s] [com]\\ik-ˈnä-lij-mənt, ak-\\[/com]
        // Abdelkader
        // [com]\\ˌab-ˌdel-ˈkä-dər\\[/com][i]or[/i] [b]Abd al-Qā·dir[/b] [s]bixabd02.wav[/s] [com]\\ˌab-dəl-\\[/com]  
        private List<MWEntry> ParseVariant(MWEntry entry, MWEntryItem lastItem, string text)
        {
            string partialText;
            bool checkParentTranscription = false;
            if (text.StartsWithOrdinal(VariantStartOr))
            {
                partialText = text.Remove(0, VariantStartOr.Length);
            }
            else if (text.StartsWithOrdinal(VariantStartAlso))
            {
                partialText = text.Remove(0, VariantStartAlso.Length);
            }
            else if (text.StartsWithOrdinal(@"[com]\"))
            {
                checkParentTranscription = true;
                partialText = text;
            }
            else
            {
                return null;
            }

            var items = new List<MWEntryItem>();
            var allSounds = new List<string>();
            var allTranscriptions = new List<string>();
            string parentTranscription = null;
            var reader = new TagReader(Trim(partialText), new string[] { "[", "]", @"\\" });
            while (true)
            {
                if (checkParentTranscription)
                {
                    if (!reader.LoadTagContent(@"[com]\\", @"\\[/com]", true, false))
                        throw new ArgumentException();

                    parentTranscription = PrepareVariantTranscription(Trim(reader.Content));
                    if (!string.IsNullOrEmpty(parentTranscription))
                    {
                        allTranscriptions.Add(parentTranscription);
                    }
                    checkParentTranscription = false;
                }

                if (!reader.LoadTagContent("[b]", "[/b]", false))
                    break;

                string transcription = null;
                var soundFiles = new List<string>();
                string variantName = PrepareFormName(Trim(reader.Content));

                if (!IsEntrySeparator(reader, true))
                {
                    while (reader.LoadTagContent("[s]", "[/s]", false, true))
                    {
                        RegisterSoundFile(reader.Content, soundFiles);
                    }
                    allSounds.AddRange(soundFiles.Where(x => !allSounds.Contains(x)));

                    if (!IsEntrySeparator(reader, true))
                    {
                        if (reader.LoadTagContent(@"[com]\\", @"\\[/com]", true, false))
                        {
                            transcription = PrepareVariantTranscription(Trim(reader.Content));
                            if (!string.IsNullOrEmpty(transcription))
                            {
                                allTranscriptions.Add(transcription);
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(variantName))
                {
                    if (soundFiles.Count == 0 && !string.IsNullOrEmpty(transcription))
                    {
                        var previousItem = items.LastOrDefault();
                        if (previousItem != null && previousItem.SoundFiles.Count > 0 
                            && string.IsNullOrEmpty(previousItem.Transcription))
                        {
                            previousItem.Transcription = transcription;
                            soundFiles = previousItem.SoundFiles;
                        }
                    }

                    if (soundFiles.Count > 0)
                    {
                        items.Add(new MWEntryItem
                        {
                            ItemTitle = PrepareTitle(variantName),
                            SoundFiles = soundFiles,
                            Transcription = transcription,
                            RawData = text
                        });
                    }
                }
            }

            // Ensure that we haven't skipped any sounds or transcription
            ValidateSoundFiles(reader, allSounds);
            ValidateTranscriptions(reader, allTranscriptions, false, PrepareVariantTranscription);

            if (!string.IsNullOrEmpty(parentTranscription) && lastItem != null && string.IsNullOrEmpty(lastItem.Transcription))
            {
                lastItem.Transcription = parentTranscription;
            }
            if (items.Count == 0)
                return null;

            if (lastItem == null)
                throw new ArgumentNullException();

            var entries = new List<MWEntry>();
            foreach (var item in items)
            {
                string keyword = PrepareKeywordFromTitle(item.ItemTitle);
                if (keyword == entry.Keyword)
                    throw new ArgumentException();

                bool isMerge = false;
                if ((lastItem.SoundFiles == null || lastItem.SoundFiles.Count == 0) && string.IsNullOrEmpty(lastItem.Transcription))
                {
                    isMerge = true;
                    lastItem.SoundFiles = item.SoundFiles;
                }
                else if (SoundsEqual(lastItem.SoundFiles, item.SoundFiles)
                    && (string.IsNullOrEmpty(lastItem.Transcription) || string.IsNullOrEmpty(item.Transcription) 
                        || lastItem.Transcription == item.Transcription))
                {
                    isMerge = true;
                }

                if (isMerge)
                {
                    MergeTitles(lastItem, item, false);

                    if (string.IsNullOrEmpty(lastItem.Transcription))
                    {
                        lastItem.Transcription = item.Transcription;
                    }
                    else if (string.IsNullOrEmpty(item.Transcription))
                    {
                        item.Transcription = lastItem.Transcription;
                    }

                    if (item.PartsOfSpeech == null || item.PartsOfSpeech.Count == 0)
                    {
                        item.PartsOfSpeech = lastItem.PartsOfSpeech;
                    }
                }

                entries.Add(new MWEntry
                {
                    Keyword = keyword,
                    Title = item.ItemTitle,
                    IsVariant = true,
                    Items = new List<MWEntryItem> { item }
                });
            }

            return entries.Count == 0 ? null : entries;
        }

        private void MergeTitles(MWEntryItem target, MWEntryItem source, bool oneWayOnly)
        {
            if (target.ItemTitle == null && source.ItemTitle == null)
                return;

            if (target.ItemTitle == null)
            {
                target.ItemTitle = source.ItemTitle.Clone();
            }
            else if (source.ItemTitle == null)
            {
                if (!oneWayOnly)
                {
                    source.ItemTitle = target.ItemTitle.Clone();
                }
            }
            else
            {
                target.ItemTitle.Merge(source.ItemTitle);
                if (!oneWayOnly)
                {
                    // This will ensure that titles order is the same in both articles
                    source.ItemTitle = target.ItemTitle.Clone();
                }
            }
        }
        
        // abase
        // [m1]• [b]abase·ment[/b] [s]abasem01.wav[/s] [com]\\-ˈbās-mənt\\[/com] [i][c][com]noun[/com][/c][/i][/m]
        private List<MWEntry> ParseDerivedWords(MWEntry parentEntry, string text, out List<MWEntryItem> derivedItems)
        {
            derivedItems = null;
            var parentReader = new TagReader(text);
            if (!parentReader.LoadTagContent("[m1]•", "[/m]", true))
                throw new ArgumentException();

            var items = new List<MWEntryItem>();
            var allSounds = new List<string>();
            var allTranscriptions = new List<string>();
            var reader = new TagReader(Trim(parentReader.Content), new string[] { "[", "]", @"\\" });
            while (true)
            {
                if (!reader.LoadTagContent("[b]", "[/b]", false))
                    break;

                string transcription = null;
                var soundFiles = new List<string>();
                string name = PrepareFormName(Trim(reader.Content));

                if (!IsEntrySeparator(reader, false))
                {
                    while (reader.LoadTagContent("[s]", "[/s]", false, true))
                    {
                        RegisterSoundFile(reader.Content, soundFiles);
                    }
                    allSounds.AddRange(soundFiles.Where(x => !allSounds.Contains(x)));

                    if (!IsEntrySeparator(reader, false))
                    {
                        if (reader.LoadTagContent(@"[com]\\", @"\\[/com]", true, false))
                        {
                            transcription = PrepareTranscription(Trim(reader.Content));
                            if (!string.IsNullOrEmpty(transcription))
                            {
                                allTranscriptions.Add(transcription);
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(name) && soundFiles.Count > 0)
                {
                    items.Add(new MWEntryItem
                    {
                        ItemTitle = PrepareTitle(name),
                        SoundFiles = soundFiles,
                        Transcription = transcription,
                        RawData = text
                    });
                }
            }

            if (reader.LoadTagContent("[i][c][com]", "[/com][/c][/i]", false))
            {
                List<string> partsOfSpeech = PrepareParstOfSpeech(Trim(reader.Content));
                foreach (var item in items)
                {
                    item.PartsOfSpeech = partsOfSpeech;
                }
            }

            // Ensure that we haven't skipped any sounds or transcription
            ValidateSoundFiles(reader, allSounds);
            ValidateTranscriptions(reader, allTranscriptions, false, null);

            if (items.Count == 0)
                return null;

            var entries = new List<MWEntry>();
            derivedItems = new List<MWEntryItem>();
            foreach (var item in items)
            {
                string keyword = PrepareKeywordFromTitle(item.ItemTitle);

                // If item keyword matches the keyword of the parent entry
                if (keyword == parentEntry.Keyword)
                {
                    derivedItems.Add(item);
                }
                else
                {
                    entries.Add(new MWEntry
                    {
                        Keyword = keyword,
                        Title = item.ItemTitle,
                        IsDerived = true,
                        Items = new List<MWEntryItem> { item }
                    });
                }
            }

            return entries.Count == 0 ? null : entries;
        }

        // [m1] Inés [s]bixcas18.wav[/s] [com]\\ē-ˈnās\\[/com] [trn] de 1320?-1355 Spanish noblewoman[/trn][/m]
        // [m1][b][c darkslategray]2.[/c][/b] [com]\\ˌa-bər-ˈdēn\\[/com] [i][c teal]or[/c][/i] [b]Ab·er·deen·shire[/b] [s]ggaber05.wav[/s] [com]\\-ˌshir, -shər\\[/com][trn] administrative area NE Scotland [i]area[/i] 2439 [i]square miles[/i] (6318 [i]square kilometers[/i])[/trn][/m]
        // [m1][trn] name of 6 popes: especially [b]IV[/b] ([i]Nicholas[/i][/trn] [i]Break·spear[/i] [s]bixadr02.wav[/s] [com]\\ˈbrāk-ˌspir\\[/com] [trn]) 1100?-1159 the only English pope (1154-59)[/trn][/m]
        private List<MWEntry> ParseExtraWords(string text)
        {
            var parentReader = new TagReader(text);
            if (!parentReader.LoadTagContent("[m1]", "[/m]", true))
                throw new ArgumentException();

            List<MWEntry> entries = new List<MWEntry>();
            int entryPosition = 0;
            var reader = new TagReader(Trim(parentReader.Content));
            while (reader.LoadTagContent("[s]", "[/s]", false))
            {
                var soundFiles = new List<string>();
                RegisterSoundFile(reader.Content, soundFiles);
                if (soundFiles.Count == 0)
                    continue;

                var skippedText = reader.SkippedText.Substring(entryPosition);
                skippedText = PrepareSkippedText(skippedText.Replace(string.Format("[s]{0}[/s]", reader.Content), null));
                if (string.IsNullOrWhiteSpace(skippedText))
                    continue;

                while (reader.LoadTagContent("[s]", "[/s]", false, true))
                {
                    RegisterSoundFile(reader.Content, soundFiles);
                }

                string transcription = null;
                if (reader.LoadTagContent(@"[com]\\", @"\\[/com]", true, true))
                {
                    transcription = PrepareTranscription(Trim(reader.Content));
                }
                entryPosition = reader.CurrentIndex;

                string name = null;
                if (!skippedText.Contains("[") && !skippedText.Contains("]"))
                {
                    name = Trim(skippedText);
                }
                else
                {
                    name = LoadLastTag(skippedText, "[b]", "[/b]");
                    if (string.IsNullOrEmpty(name))
                    {
                        name = LoadLastTag(skippedText, "[i]", "[/i]");
                    }
                }

                if (string.IsNullOrEmpty(name) || name.Contains("(") || name.Contains(")") || name.Length == 1)
                    continue;

                var title = PrepareExtraTitle(name);
                entries.Add(new MWEntry
                {
                    Keyword = PrepareKeywordFromTitle(title),
                    Title = title,
                    IsDerived = true,
                    Items = new List<MWEntryItem> 
                    { 
                        new MWEntryItem
                        {
                            ItemTitle = title,
                            SoundFiles = soundFiles,
                            Transcription = transcription,
                            RawData = text
                        } 
                    }
                });
            }

            return entries.Count == 0 ? null : entries;
        }

        private string PrepareSkippedText(string skippedText)
        {
            if (string.IsNullOrEmpty(skippedText))
                return skippedText;

            var replacements = new List<string>();
            var reader = new TagReader(skippedText);
            while (reader.LoadTagContent("[trn]", "[/trn]", true))
            {
                replacements.Add(string.Format("[trn]{0}[/trn]", reader.Content));
            }

            if (replacements.Count == 0)
                return skippedText;

            foreach (var replacement in replacements)
            {
                skippedText = skippedText.Replace(replacement, null);
            }

            return skippedText;
        }

        private string LoadLastTag(string text, string openTag, string closeTag)
        {
            var reader = new TagReader(text);
            string content = null;
            while (reader.LoadTagContent(openTag, closeTag, true, false))
            {
                content = Trim(reader.Content);
            }

            if (!string.IsNullOrEmpty(content) && string.IsNullOrWhiteSpace(reader.RemainingText))
                return content;

            return null;
        }

        private bool IsEntrySeparator(TagReader reader, bool isExtendedTest)
        {
            if (reader.LoadTagContent("[i]", "[/i]", true, true))
            {
                var content = Trim(reader.Content);
                if (content == "or" || content == "also")
                    return true;

                if (isExtendedTest && (content == "or formerly" || content == "Welsh"))
                    return true;
            }

            return false;
        }

        private void ValidateSoundFiles(TagReader reader, List<string> soundFiles)
        {
            reader.ResetPosition();
            var allSounds = CollectSoundFiles(reader);
            if ((allSounds == null ? 0 : allSounds.Count) != (soundFiles == null ? 0 : soundFiles.Count))
                throw new ArgumentException();
        }

        private void ValidateTranscription(TagReader reader, string transcription, bool useShortTags)
        {
            string firstTranscription = null;
            string startTag = useShortTags ? @"\\" : @"[com]\\";
            string endTag = useShortTags ? @"\\" : @"\\[/com]";

            reader.ResetPosition();
            while (reader.LoadTagContent(startTag, endTag, true))
            {
                if (!string.IsNullOrEmpty(firstTranscription))
                    throw new ArgumentException();

                firstTranscription = PrepareTranscription(Trim(reader.Content));
            }

            if (firstTranscription != transcription)
                throw new ArgumentException();
        }

        private void ValidateTranscriptions(TagReader reader, List<string> transcriptions, bool useShortTags, 
            Func<string, string> tranBuilder)
        {
            string startTag = useShortTags ? @"\\" : @"[com]\\";
            string endTag = useShortTags ? @"\\" : @"\\[/com]";

            reader.ResetPosition();
            int count = 0;
            while (reader.LoadTagContent(startTag, endTag, true))
            {
                string transcription = tranBuilder == null ? PrepareTranscription(Trim(reader.Content)) : tranBuilder(Trim(reader.Content));
                if (!string.IsNullOrEmpty(transcription))
                {
                    count++;
                    if(transcription != transcriptions[count - 1])
                        throw new ArgumentException();
                }
            }

            if (count != transcriptions.Count)
                throw new ArgumentException();
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
                if (item.IsRawTranscription)
                {
                    if (!ResolveTranscriptionReference(item, entry.Items))
                    {
                        item.Transcription = PrepareTranscription(item.Transcription);
                    }
                    item.IsRawTranscription = false;
                }

                MWEntryItem matchedItem = null;
                if (string.IsNullOrEmpty(item.Transcription))
                {
                    if (!item.HasSounds)
                    {
                        matchedItem = items.LastOrDefault();
                        if (matchedItem == null)
                            continue;
                    }
                    else
                    {
                        matchedItem = items.FirstOrDefault(x => SoundsEqual(x.SoundFiles, item.SoundFiles));
                    }
                }
                else
                {
                    matchedItem = items.FirstOrDefault(x => x.Transcription == item.Transcription);
                }

                if (matchedItem == null)
                {
                    items.Add(item);
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

            if (!item.Transcription.StartsWithOrdinal("[i]same as"))
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

        private string PrepareVariantTranscription(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            if (text.StartsWithOrdinal("[i]same as[/i]") || text == "[i]same[/i]")
                return null;

            var starts = new string[] { "[i]all[/i]", "[i]also[/i]", "[i]same or[/i]", "[i]same, or[/i]" };
            foreach (var start in starts)
            {
                if (text.StartsWithOrdinal(start))
                {
                    text = Trim(text.Remove(0, start.Length));
                    break;
                }
            }

            return PrepareTranscription(text);
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
            return PrepareKeywordFromTitle(PrepareTitle(text));
        }

        private string PrepareKeywordFromTitle(DisplayName title)
        {
            if (title == null)
                return null;

            var titleText = title.ToString();
            if (!string.IsNullOrEmpty(titleText))
            {
                titleText = titleText.Replace("<sub>", "").Replace("</sub>", "");
            }

            return titleText;
        }

        private DisplayName PrepareTitle(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            return new DisplayName(Trim(text).Replace("{·}", "")
                .Replace(@"\", "")
                .Replace("{[sub]}", "<sub>")
                .Replace("{[/sub]}", "</sub>"));
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

        private DisplayName PrepareExtraTitle(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            text = Trim(text.Replace("·", "")
                .Replace("[sub]", "<sub>")
                .Replace("[/sub]", "</sub>"));

            if (text.Contains("[") || text.Contains(@"\") || text.Contains("|"))
                throw new ArgumentException();

            return new DisplayName(text);
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

            target.SoundFiles = MergeSoundFiles(target.SoundFiles, source.SoundFiles);

            if (source.WordForms != null && source.WordForms.Count > 0)
            {
                if (target.WordForms == null)
                {
                    target.WordForms = source.WordForms;
                }
                else
                {
                    foreach (var sourceForm in source.WordForms)
                    {
                        var matchingForm = target.WordForms.FirstOrDefault(x => x.IsPluralForm == sourceForm.IsPluralForm 
                            && x.Title.IsEqual(sourceForm.Title));
                        if (matchingForm != null)
                        {
                            if (matchingForm.Transcription != sourceForm.Transcription)
                            {
                                if (IncludesText(matchingForm.Transcription, sourceForm.Transcription))
                                {
                                }
                                else if (IncludesText(sourceForm.Transcription, matchingForm.Transcription))
                                {
                                    matchingForm.Transcription = sourceForm.Transcription;
                                }
                                else if (matchingForm.Title.ToString() == "calves")
                                {
                                }
                                else
                                {
                                    matchingForm = null;
                                }
                            }
                        }

                        if (matchingForm != null)
                        {
                            matchingForm.SoundFiles = MergeSoundFiles(matchingForm.SoundFiles, sourceForm.SoundFiles);
                        }
                        else
                        {
                            target.WordForms.Add(sourceForm);
                        }
                    }
                }
            }
        }

        private List<string> MergeSoundFiles(List<string> target, List<string> source)
        {
            if (source == null)
                return target;

            if (target == null)
                return source;

            // Do not actually merge sounds because they order often matches the transcription
            // so we just get either target or source sounds.
            if (target.Count == 0 && source.Count > 0)
            {
                target.AddRange(source);
            }

            return target;
        }

        private bool IncludesText(string baseText, string searchedText)
        {
            if (string.IsNullOrEmpty(searchedText) || searchedText == baseText)
                return true;

            if (string.IsNullOrEmpty(baseText))
                return false;

            if (searchedText.StartsWithOrdinal("-") && searchedText.EndsWith("-"))
            {
                return baseText.Contains(searchedText.Remove(searchedText.Length - 1, 1).Remove(0, 1));
            }
            else if (searchedText.StartsWithOrdinal("-"))
            {
                return baseText.EndsWith(searchedText.Remove(0, 1));
            }
            else if (searchedText.EndsWith("-"))
            {
                return baseText.StartsWithOrdinal(searchedText.Remove(searchedText.Length - 1, 1));
            }
            else
            {
                return baseText.StartsWithOrdinal(searchedText);
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
            if (keyword.ToLower().EndsWith(TheEnding))
            {
                keyword = Trim(keyword.Substring(0, keyword.Length - TheEnding.Length));
            }

            return keyword;
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
