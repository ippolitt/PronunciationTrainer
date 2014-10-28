using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Data.SqlServerCe;
using System.Data;

namespace Pronunciation.Parser
{
    class HtmlBuilder
    {
        public enum GenerationMode
        {
            Database,
            FileSystem,
            IPhone
        }

        private class WordGroup
        {
            public string Name;
            public List<DicWord> Words;
        }

        public class WordAudio
        {
            public string SoundTextUK;
            public string SoundTextUS;
        }

        public class ParseResult
        {
            public string HtmlData;

            public string SoundTextUK;
            public string SoundTextUS;
        }

        private readonly WordUsageBuilder _usageBuilder;
        private readonly Dictionary<int, int> _ranks;
        private readonly LDOCEHtmlBuilder _ldoce;
        private readonly MWHtmlBuilder _mw;
        private readonly DatabaseUploader _dbUploader;
        private readonly FileNameBuilder _nameBuilder;
        private readonly XmlReplaceMap _replaceMap;
        private readonly string _logFile;
        private readonly string _binFolder;
        private readonly GenerationMode _generationMode;
        private readonly AudioButtonHtmlBuilder _buttonBuilder;
        private readonly IFileLoader _fileLoader;

        private const string TemplateFilePath = @"Html\FileTemplate.html";
        private const string TopTemplateFilePath = @"Html\TopTemplate.html";
        private const string RootPath = "../../";
        private const string ImagesPath = "Images/";
        private const string DicPath = "Dic/";
        private const string DicFolderName = "Dic";
        private const string IndexFileName = "Index.txt";

        private bool IsDatabaseMode
        {
            get { return _generationMode == GenerationMode.Database; }
        }

        private bool GenerateTopWordsNavigation
        {
            get { return _generationMode == GenerationMode.IPhone; }
        }

        private bool GenerateTopWordsList
        {
            get { return _generationMode == GenerationMode.IPhone; }
        }

        public HtmlBuilder(GenerationMode generationMode, DatabaseUploader dbUploader, 
            AudioButtonHtmlBuilder buttonBuilder, IFileLoader fileLoader,
            LDOCEHtmlBuilder ldoce, MWHtmlBuilder mw, WordUsageBuilder usageBuilder, string logFile)
        {
            _generationMode = generationMode;
            _logFile = logFile;
            _ldoce = ldoce;
            _mw = mw;
            _dbUploader = dbUploader;
            _usageBuilder = usageBuilder;
            _buttonBuilder = buttonBuilder;
            _fileLoader = fileLoader;

            _ranks = PrepareRanks(_usageBuilder.GetRanks());
            _replaceMap = new XmlReplaceMap();
            _nameBuilder = new FileNameBuilder();
            _binFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        private Dictionary<int, int> PrepareRanks(int[] ranks)
        {
            var rankText = new Dictionary<int, int>();
            int? previousRank = null;
            foreach (int rank in ranks.OrderBy(x => x))
            {
                rankText.Add(rank, previousRank ?? 0);
                previousRank = rank;
            }

            return rankText;
        }

        public void ConvertToHtml(string sourceXml, string htmlFolder, int maxWords, bool isFakeMode, bool deleteExtraWords)
        {
            File.WriteAllText(_logFile, string.Format("*** Starting conversion {0} ***\r\n\r\n", DateTime.Now));

            List<DicWord> words = ParseFile(sourceXml);
            SplitSynonyms(words);
            if (IsDatabaseMode)
            {
                AddCollocations(words);
            }

            EntriesMapper mapper = new EntriesMapper(words);
            mapper.AddEntries(_ldoce, IsDatabaseMode);
            mapper.AddEntries(_mw, IsDatabaseMode);
            if (mapper.Stats != null)
            {
                File.AppendAllText(_logFile, "\r\nExtra entries matching stats:\r\n" + mapper.Stats.ToString());
            }

            _usageBuilder.Initialize(words);

            var soundStats = new StringBuilder();
            if (IsDatabaseMode)
            {
                GenerateInDatabase(words, soundStats, maxWords, isFakeMode, deleteExtraWords);
            }
            else
            {
                GenerateInFileSystem(words, htmlFolder, soundStats, maxWords, isFakeMode);
            }

            File.AppendAllText(_logFile, "\r\nSound stats:\r\n" + soundStats.ToString());

            if (_usageBuilder.Stats != null)
            {
                File.AppendAllText(_logFile, "\r\nWord usage stats:\r\n" + _usageBuilder.Stats.ToString());
            }

            if (GenerateTopWordsList)
            {
                GenerateTopList(htmlFolder);
                Console.WriteLine("Generated top words lists");
            }

            File.AppendAllText(_logFile, "Ended conversion.\r\n\r\n");
        }

        private void SplitSynonyms(List<DicWord> words)
        {
            // Disable content loading to speed up HTML generation inside "ParseItemXml" method
            _buttonBuilder.IsContentLoadDisabled = true;
            try
            {
                var keywords = new HashSet<string>(words.Select(x => x.Keyword), StringComparer.OrdinalIgnoreCase);
                List<DicWord> extraWords = new List<DicWord>();
                foreach (var word in words)
                {
                    foreach (DicEntry entry in word.LPDEntries)
                    {
                        SplitSynonyms(entry, extraWords, keywords);
                    }
                }

                words.AddRange(extraWords);

                File.AppendAllText(_logFile, string.Join(Environment.NewLine, extraWords
                    .OrderBy(x => x.Keyword)
                    .Select(x => string.Format("Extracted synonym '{0}'.", x.Keyword))));
                File.AppendAllText(_logFile, string.Format("\r\n\r\nExtracted {0} synonyms as words.\r\n\r\n", extraWords.Count));
            }
            finally
            {
                _buttonBuilder.IsContentLoadDisabled = false;
            }
        }

        private void SplitSynonyms(DicEntry entry, List<DicWord> words, HashSet<string> keywords)
        {
            if (string.IsNullOrEmpty(entry.RawMainData))
                return;

            var contentCollector = new XmlContentCollector(XmlReplaceMap.XmlElementEntryName, true);
            ParseItemXml(entry.RawMainData, false, contentCollector, null);

            var names = PrepareWordNames(contentCollector.GetContent());
            if (names == null || names.Count <= 1)
                return;

            // Start from the second name
            for(int i = 1; i < names.Count; i++)
            {
                if (keywords.Add(names[i]))
                {
                    words.Add(new DicWord
                    {
                        Title = names[i],
                        LPDEntries = new List<DicEntry> 
                        { 
                            new DicEntry { RawMainData = entry.RawMainData, AllItems = entry.AllItems } 
                        }
                    });
                }
            }
        }

        private void AddCollocations(List<DicWord> words)
        {
            // Disable content loading to speed up HTML generation inside "ParseItemXml" method
            _buttonBuilder.IsContentLoadDisabled = true;
            try
            {
                var keywords = new HashSet<string>(words.Select(x => x.Keyword), StringComparer.OrdinalIgnoreCase);
                List<DicWord> extraWords = new List<DicWord>();
                foreach (var word in words)
                {
                    foreach (DicEntry entry in word.LPDEntries)
                    {
                        AddCollocations(entry, extraWords, keywords);
                    }
                }

                words.AddRange(extraWords);

                File.AppendAllText(_logFile, string.Join(Environment.NewLine, extraWords
                    .OrderBy(x => x.Keyword)
                    .Select(x => string.Format("Extracted collocation '{0}'.", x.Keyword))));
                File.AppendAllText(_logFile, 
                    string.Format("\r\n\r\nExtracted {0} collocations as words.\r\n\r\n", extraWords.Count));
            }
            finally
            {
                _buttonBuilder.IsContentLoadDisabled = false;
            }
        }

        private void AddCollocations(DicEntry entry, List<DicWord> words, HashSet<string> keywords)
        {
            if (entry.AllItems == null || entry.AllItems.Count <= 0)
                return;
           
            foreach (var item in entry.AllItems.Where(x => x.ItemType == ItemType.Collocation))
            {
                var contentCollector = new XmlContentCollector(XmlReplaceMap.XmlElementCollocationName, false);
                ParseItemXml(item.RawData, false, contentCollector, null);

                var names = PrepareCollocationNames(contentCollector.GetContent());
                if (names == null || names.Count == 0)
                    continue;

                foreach (var name in names)
                {
                    if (keywords.Add(name))
                    {
                        words.Add(new DicWord
                        {
                            Title = name,
                            IsLPDCollocation = true,
                            LPDEntries = new List<DicEntry> 
                            { 
                                new DicEntry { RawMainData = _replaceMap.ConvertCollocationToWord(item.RawData) } 
                            }
                        });

                    }
                }
            }
        }

        private void GenerateInDatabase(List<DicWord> words, StringBuilder soundStats, int maxWords, bool isFakeMode, bool deleteExtraWords)
        {
            int wordsCount = 0;
            int letterWords = 0;
            string currentLetter = null;

            // Group by keyword (this is required because Sound files cache depends on this keyword)
            var groups = GroupWords(words, null);

            _dbUploader.Open();
            try
            {
                var sounds = new List<SoundInfo>();
                foreach (var group in groups.Values.OrderBy(x => x.Name, StringComparer.Ordinal))
                {
                    var letter = group.Name.Substring(0, 1);
                    if (currentLetter != letter)
                    {
                        if (letterWords > 0)
                        {
                            Console.WriteLine(" ({0}) words.", letterWords);
                            letterWords = 0;
                        }
                        Console.Write("Processing " + letter);
                        currentLetter = letter;
                    }

                    foreach (var word in group.Words)
                    {
                        var pageBuilder = new StringBuilder();
                        WordDescription description = GenerateHtml(word, pageBuilder, soundStats);
                        description.DictionaryId = word.DictionaryId;
                        if (description.Sounds != null)
                        {
                            sounds.AddRange(description.Sounds);
                        }

                        if (!isFakeMode)
                        {
                            _dbUploader.StoreWord(description, pageBuilder.ToString());
                        }

                        letterWords++;
                        wordsCount++;
                    }

                    if (maxWords > 0 && wordsCount >= maxWords)
                        break;
                }

                Console.WriteLine(" ({0}) words.", letterWords);
                Console.WriteLine("Total: {0} words", wordsCount);

                if (!isFakeMode)
                {
                    Console.WriteLine("Processing sounds...");
                    int soundsCount = _dbUploader.StoreSounds(sounds);
                    Console.WriteLine("Totally processed {0} sounds.", soundsCount);

                    if (deleteExtraWords)
                    {
                        int deleteCount = _dbUploader.DeleteExtraWords();
                        Console.WriteLine("Deleted '{0}' extra words", deleteCount);
                    }
                }

                _dbUploader.FinishUpload();
                if (_dbUploader.DbStats != null)
                {
                    File.AppendAllText(_logFile, "\r\nDatabase upload stats:\r\n" + _dbUploader.DbStats.ToString());
                }
            }
            finally
            {
                _dbUploader.Dispose();
            }
        }

        private void GenerateInFileSystem(List<DicWord> words, string rootFolder, StringBuilder soundStats, int maxWords, bool isFakeMode)
        {
            // Load template
            var template = File.ReadAllText(Path.Combine(_binFolder, TemplateFilePath), Encoding.UTF8);

            // Group by keyword
            var groups = GroupWords(words, new StringBuilder());
            var dicFolder = Path.Combine(rootFolder, DicFolderName);
            if (!Directory.Exists(dicFolder))
            {
                Directory.CreateDirectory(dicFolder);
            }

            int wordsCount = 0;
            int letterGroups = 0;
            string outputFolder = null;
            var titleStats = new StringBuilder();
            var letterStats = new StringBuilder();
            var indexBuilder = new StringBuilder();
            foreach (var group in groups.Values.OrderBy(x => x.Name, StringComparer.Ordinal))
            {
                var subFolder = group.Name.Substring(0, 1);
                if (Path.Combine(dicFolder, subFolder) != outputFolder)
                {
                    outputFolder = Path.Combine(dicFolder, subFolder);
                    if (!Directory.Exists(outputFolder))
                    {
                        Directory.CreateDirectory(outputFolder);
                    }

                    if (letterGroups > 0)
                    {
                        letterStats.AppendFormat(", groups: {0}\r\n", letterGroups);
                        Console.WriteLine(" ({0}) groups.", letterGroups);

                        letterGroups = 0;
                    }

                    letterStats.AppendFormat("Letter: {0}", subFolder);
                    Console.Write("Processing " + subFolder);
                }

                var pageBuilder = new StringBuilder();
                var sounds = new Dictionary<string, string>();
                foreach (var word in group.Words)
                {
                    WordDescription description = GenerateHtml(word, pageBuilder, soundStats);
                    int? usageRank = description.UsageInfo == null ? (int?)null : description.UsageInfo.CombinedRank;
                    indexBuilder.AppendLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}",
                        description.Text, group.Name, usageRank,
                        description.SoundKeyUK, description.SoundKeyUS, word.DictionaryId));

                    if (description.Sounds != null)
                    {
                        foreach (var sound in description.Sounds)
                        {
                            if (sounds.ContainsKey(sound.SoundKey))
                                continue;

                            sounds.Add(sound.SoundKey, _fileLoader.GetBase64Content(sound.SoundKey));
                        }
                    }
                }

                // Generate title
                string title = string.Join(" + ", group.Words.Select(x => x.Keyword));
                if (group.Words.Count > 1)
                {
                    titleStats.AppendFormat("Grouped words: {0}\r\n", title);
                }

                if (!isFakeMode)
                {
                    File.WriteAllText(Path.Combine(outputFolder, string.Format("{0}.html", group.Name)),
                        string.Format(template, title, pageBuilder, PrepareJScriptSounds(sounds)),
                        Encoding.UTF8);
                }

                letterGroups++;
                wordsCount += group.Words.Count; 
                if (maxWords > 0 && wordsCount >= maxWords)
                    break;
            }

            letterStats.AppendFormat(", groups: {0}\r\n", letterGroups);
            Console.WriteLine(" ({0}) groups.", letterGroups);
            Console.WriteLine("Total: {0} words, {1} groups", wordsCount, groups.Count);

            File.AppendAllText(_logFile, "\r\nTitle stats:\r\n" + titleStats.ToString());
            File.AppendAllText(_logFile, "\r\nLetter stats:\r\n" + letterStats.ToString());

            // Store index file
            File.WriteAllText(Path.Combine(rootFolder, IndexFileName), indexBuilder.ToString(), Encoding.UTF8);
            Console.WriteLine("Created index file '{0}'", IndexFileName);
        }

        private Dictionary<string, WordGroup> GroupWords(List<DicWord> words, StringBuilder nameStats)
        {
            var groups = new Dictionary<string, WordGroup>();
            foreach (var word in words)
            {
                if ((word.DictionaryId ?? 0) == 0  && word.LPDEntries.Count <= 0)
                    throw new ArgumentException();

                string groupName = _nameBuilder.PrepareFileName(word.Keyword, nameStats);
                WordGroup group;
                if (!groups.TryGetValue(groupName, out group))
                {
                    group = new WordGroup { Name = groupName, Words = new List<DicWord>() };
                    groups.Add(groupName, group);
                }

                group.Words.Add(word);
            }

            if (nameStats != null)
            {
                File.AppendAllText(_logFile, "\r\nName stats:\r\n" + nameStats.ToString());
            }

            return groups;
        }

        private WordDescription GenerateHtml(DicWord word, StringBuilder pageBuilder, StringBuilder soundStats)
        {
            var wordDescription = new WordDescription 
            { 
                Text = word.Keyword,
                IsCollocation = word.IsLPDCollocation,
                UsageInfo = _usageBuilder.GetUsage(word.Keyword)
            };

            WordAudio wordAudio = null;
            string lpdHtml = null;
            if (word.LPDEntries != null)
            {
                lpdHtml = GenerateLPDHtml(word.Keyword, word.LPDEntries, wordDescription, soundStats, out wordAudio);
            }

            string ldoceHtml = null;
            if (word.LDOCEEntry != null)
            {
                if (word.LPDEntries == null)
                {
                    ldoceHtml = _ldoce.GeneratePageHtml(word.LDOCEEntry, wordDescription, out wordAudio);
                }
                else
                {
                    ldoceHtml = _ldoce.GenerateFragmentHtml(word.LDOCEEntry, wordDescription);
                }
            }

            string mwHtml = null;
            if (word.MWEntry != null)
            {
                if (word.LPDEntries == null && word.LDOCEEntry == null)
                {
                    mwHtml = _mw.GeneratePageHtml(word.MWEntry, wordDescription, out wordAudio);
                }
                else
                {
                    mwHtml = _mw.GenerateFragmentHtml(word.MWEntry, wordDescription);
                }
            }

            pageBuilder.AppendFormat(
@"  <div class=""word"">
        <span class=""word_name"">{0}</span>
",
                word.Title);

            if (wordDescription.UsageInfo != null && wordDescription.UsageInfo.CombinedRank > 0)
            {
                pageBuilder.Append(GenerateUsageInfoHtml(wordDescription.UsageInfo));
            }

            if (word.Language != null)
            {
                pageBuilder.AppendFormat(
@"      <div class=""lang_variant"">{0}</div>
",
                    PrepareLanguageVariant(word.Language.Value));
            }

            if (wordAudio != null)
            {
                if (_generationMode != GenerationMode.IPhone)
                    throw new ArgumentException();

                pageBuilder.AppendFormat(
@"      <div class=""word_audio"">
            {0}
            {1}
        </div>",
                    wordAudio.SoundTextUK, wordAudio.SoundTextUS);
            }

            if (_generationMode == GenerationMode.Database)
            {
                pageBuilder.Append(
@"      <div id=""customNotesContainer""></div>");
            }

            pageBuilder.AppendFormat(
@"{0}{1}{2}
</div>  
",
                lpdHtml, ldoceHtml, mwHtml);

            return wordDescription;
        }

        private string GenerateUsageInfoHtml(WordUsageInfo rank)
        {
            if (GenerateTopWordsNavigation)
            {
                return string.Format(
@"      <span class=""word_usage"">Usage TOP: <strong>{0}</strong>{1}{2}</span>
",
                    rank.CombinedRank,
                    rank.PreviousWord == null ? null :
                        string.Format("<a class=\"word_link previous_word\" href=\"{0}\">&lt; [{1}]</a>",
                        PrepareLink(rank.PreviousWord.Keyword, false), rank.PreviousWord.Keyword),
                    rank.NextWord == null ? null :
                        string.Format("<a class=\"word_link next_word\" href=\"{0}\">[{1}] &gt;</a>",
                        PrepareLink(rank.NextWord.Keyword, false), rank.NextWord.Keyword));
            }
            else
            {
                int previousRank =  _ranks[rank.CombinedRank];
                return string.Format(
@"      <span class=""word_usage"">Usage rank: top {0}{1}</span>
",
                    previousRank == 0 ? null : string.Format(@"<span class=""word_usage_from"">{0}</span>", previousRank),
                    string.Format(@"<span class=""word_usage_to"">{0}</span>", rank.CombinedRank));
            }
        }

        private string PrepareLanguageVariant(EnglishVariant language)
        {
            string result;
            switch (language)
            {
                case EnglishVariant.British:
                    result = "British";
                    break;
                case EnglishVariant.American:
                    result = "American";
                    break;
                case EnglishVariant.Australian:
                    result = "Australian";
                    break;
                default:
                    throw new NotSupportedException();
            }

            return result + " English";
        }

        private string GenerateLPDHtml(string keyword, List<DicEntry> entries, WordDescription wordDescription,
            StringBuilder soundStats, out WordAudio wordAudio)
        {
            wordAudio = null;
            if (entries.Count > 1)
            {
                wordDescription.HasMultiplePronunciations = true;
            }
            bool extractSounds = _generationMode == GenerationMode.IPhone;
            bool joinNumberWithEntry = _generationMode != GenerationMode.IPhone;

            int ordinal = 0;
            var bld = new StringBuilder();
            foreach (var entry in entries)
            {
                ordinal++;

                // Parse main data
                var entrySoundCollector = new SoundCollector();
                ParseResult entryResult = ParseItemXml(entry.RawMainData, extractSounds, null, entrySoundCollector);
                wordDescription.Sounds.AddRange(entrySoundCollector.Sounds);

                // If word entry has exactly two sounds (UK & US) then extraction is acceptable
                // otherwise rebuild output html without extracting the audio
                if (extractSounds && (!entrySoundCollector.HasUKSound || !entrySoundCollector.HasUSSound 
                    || entrySoundCollector.Sounds.Count != 2))
                {
                    entryResult = ParseItemXml(entry.RawMainData, false, null, null);
                }

                // Assing first sounds as the word sounds
                if (string.IsNullOrEmpty(wordDescription.SoundKeyUK))
                {
                    wordDescription.SoundKeyUK = entrySoundCollector.MainSoundUK;
                }
                if (string.IsNullOrEmpty(wordDescription.SoundKeyUS))
                {
                    wordDescription.SoundKeyUS = entrySoundCollector.MainSoundUS;
                }

                string warningText = null;
                if (!entrySoundCollector.HasUKSound && !entrySoundCollector.HasUSSound)
                {
                    warningText = "misses both UK & US pronunciations";
                }
                else if (!entrySoundCollector.HasUKSound)
                {
                    warningText = "misses a UK pronunciation";
                }
                else if (!entrySoundCollector.HasUSSound)
                {
                    warningText = "misses a US pronunciation";
                }
                else
                {
                    if (entrySoundCollector.Sounds.Count > 2)
                    {
                        warningText = "has more than two pronunciations";
                    }
                }

                if (!string.IsNullOrEmpty(warningText))
                {
                    soundStats.AppendFormat("Word entry [{0}] {1} {2}\r\n", keyword, entry.EntryNumber, warningText);
                }

                _buttonBuilder.InjectSoundText(entryResult, keyword, entry.EntryNumber, entrySoundCollector.Sounds);

                bld.Append(
@"      
    <div class=""entry"">
");

                if (joinNumberWithEntry)
                {
                    bld.Append(
@"          <div class=""entry_text"">");

                    if (entries.Count > 1)
                    {
                        bld.AppendFormat(
@"<span class=""entry_number"">{0}. </span>",
                                ordinal);
                    }

                    bld.AppendFormat(
@"{0}</div>
",
                            entryResult.HtmlData);
                }
                else
                {
                    if (string.IsNullOrEmpty(entry.EntryNumber))
                    {
                        if (entries.Count != 1)
                            throw new ArgumentException();
                    }
                    else
                    {
                        bld.AppendFormat(
@"          <span class=""entry_number"">{0}</span>
",
                            entry.EntryNumber);
                    }

                    if (!string.IsNullOrEmpty(entryResult.SoundTextUK) || !string.IsNullOrEmpty(entryResult.SoundTextUS))
                    {
                        if (entries.Count == 1)
                        {
                            // Add sounds directly to the word name if this is a single entry
                            wordAudio = new WordAudio { SoundTextUK = entryResult.SoundTextUK, SoundTextUS = entryResult.SoundTextUS };
                        }
                        else
                        {
                            bld.AppendFormat(
@"          <span class=""entry_audio"">
                {0}
                {1}
            </span>
",
                                entryResult.SoundTextUK, entryResult.SoundTextUS);
                        }
                    }

                    bld.AppendFormat(
@"          <div class=""entry_text"">{0}</div>
",
                            entryResult.HtmlData);
                }

                // Parse word forms and collocations
                if (entry.AllItems != null && entry.AllItems.Count > 0)
                {
                    // Group items
                    var itemGroups = new List<ItemGroup>();
                    entry.AllItems.ForEach(x => AddItem(itemGroups, x));

                    foreach (var itemGroup in itemGroups)
                    {
                        if (itemGroup.GroupType == ItemType.WordForm)
                        {
                            bld.Append(
@"          <div class=""forms"">
");
                        }
                        else
                        {
                            bld.Append(
@"          <div class=""collocations"">
");
                        }

                        foreach (var item in itemGroup.Items)
                        {
                            XmlContentCollector contentCollector = null;
                            if (itemGroup.GroupType == ItemType.Collocation)
                            {
                                contentCollector = new XmlContentCollector(XmlReplaceMap.XmlElementCollocationName, true);
                            }

                            var itemSoundCollector = new SoundCollector();
                            ParseResult itemResult = ParseItemXml(item.RawData, false, contentCollector, itemSoundCollector);
                            if (itemGroup.GroupType == ItemType.Collocation)
                            {
                                var contentItems = PrepareCollocationNames(contentCollector.GetContent());
                                if (contentItems != null && contentItems.Count > 0)
                                {
                                    _buttonBuilder.InjectSoundText(itemResult, 
                                        string.Join(", ", contentItems), null, itemSoundCollector.Sounds);
                                }
                            }
                            wordDescription.Sounds.AddRange(itemSoundCollector.Sounds);

                            if (itemGroup.GroupType == ItemType.WordForm)
                            {
                                bld.AppendFormat(
@"              <div class=""form"">{0}</div>
",
                                    itemResult.HtmlData);
                            }
                            else
                            {
                                bld.AppendFormat(
@"              <div class=""collocation"">{0}</div>
",
                                    itemResult.HtmlData);
                            }
                        }

                        bld.Append(
@"          </div>
");
                    }
                }

                bld.Append(
@"      </div>");
            }

            return bld.ToString();
        }

        private string PrepareImagePath(string imageName)
        {
            if (IsDatabaseMode)
            {
                return string.Format("{0}{1}", ImagesPath, imageName);
            }
            else
            {
                return string.Format("{0}{1}{2}", RootPath, ImagesPath, imageName);
            }
        }

        private string PrepareLink(string keyword, bool isRootedSource)
        {
            if (IsDatabaseMode)
            {
                return string.Format("javascript:void(loadPage('{0}'))", HtmlHelper.PrepareJScriptString(keyword));
            }
            else
            {
                var fileName = _nameBuilder.PrepareFileName(keyword, null);
                return string.Format("{0}{1}{2}/{3}.html",
                    (isRootedSource ? null : RootPath), DicPath, fileName.Substring(0, 1), fileName);
            }
        }

        private List<DicWord> ParseFile(string sourceXml)
        {
            List<DicWord> words = new List<DicWord>();
            using (var reader = XmlReader.Create(sourceXml))
            {
                reader.ScrollToRootTag(XmlBuilder.ElementRoot);

                while (!reader.EOF)
                {
                    if (reader.ScrollToOptionalStartTag(XmlBuilder.ElementKeyword))
                    {
                        var word = ProcessWord(reader);
                        words.Add(word);
                    }
                    else
                    {
                        if (reader.NodeType == XmlNodeType.EndElement && reader.Name == XmlBuilder.ElementRoot)
                        {
                            break;
                        }
                    }
                }
            }

            return words;
        }

        private ParseResult ParseItemXml(string rawData, bool extractSounds, XmlContentCollector contentCollector, 
            SoundCollector soundCollector)
        {
            if (string.IsNullOrWhiteSpace(rawData))
                throw new ArgumentException();

            var result = new ParseResult();
            var bld = new StringBuilder();
            var activeNodes = new Stack<CurrentTagInfo>();
            CurrentTagInfo activeNode = null;
            using (var reader = XmlReader.Create(new StringReader(rawData), 
                new XmlReaderSettings 
                { 
                    ConformanceLevel = ConformanceLevel.Fragment 
                }))
            {
                reader.Read();
                while (!reader.EOF)
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (activeNode != null && !activeNode.ReplaceInfo.AllowChildTags)
                                throw new ArgumentException();

                            var replace = _replaceMap.GetReplaceInfo(reader.Name);
                            if (reader.IsEmptyElement)
                            {
                                var replacement = PrepareText(replace, null, null, false);
                                if (activeNode != null)
                                {
                                    activeNode.Data += replacement;
                                }
                                else
                                {
                                    bld.Append(replacement);
                                }
                            }
                            else
                            {
                                activeNode = new CurrentTagInfo { ReplaceInfo = replace };
                                activeNodes.Push(activeNode);
                                if (contentCollector != null)
                                {
                                    contentCollector.NodeOpened(reader.Name);
                                }
                            }
                            break;

                        case XmlNodeType.EndElement:
                            if (activeNode == null)
                                throw new ArgumentException();

                            if (activeNode.ReplaceInfo.SourceTag != reader.Name)
                                throw new ArgumentException();

                            var replacementText = PrepareText(activeNode.ReplaceInfo, activeNode.Data, soundCollector, extractSounds);
                            
                            // Move main sounds (US and UK) to the outer level
                            if (extractSounds)
                            {
                                if (activeNode.ReplaceInfo.ReplacementType == ReplacementType.ReplaceSoundUK)
                                {
                                    if (string.IsNullOrEmpty(result.SoundTextUK))
                                    {
                                        result.SoundTextUK = replacementText;
                                        replacementText = null;
                                    }
                                }
                                else if (activeNode.ReplaceInfo.ReplacementType == ReplacementType.ReplaceSoundUS)
                                {
                                    if (string.IsNullOrEmpty(result.SoundTextUS))
                                    {
                                        result.SoundTextUS = replacementText;
                                        replacementText = null;
                                    }
                                }
                            }

                            activeNodes.Pop();
                            if (activeNodes.Count > 0)
                            {
                                var previousNode = activeNodes.Peek();
                                previousNode.Data += replacementText;
                                activeNode = previousNode;
                            }
                            else
                            {
                                bld.Append(replacementText);
                                activeNode = null;
                            }

                            if (contentCollector != null)
                            {
                                contentCollector.NodeClosed();
                            }
                            break;

                        case XmlNodeType.Whitespace:
                        case XmlNodeType.Text:
                            if (activeNode == null)
                            {
                                bld.Append(reader.Value);
                            }
                            else
                            {
                                activeNode.Data += reader.Value;
                            }

                            if (contentCollector != null)
                            {
                                if (!((reader.Value == "(" || reader.Value == ")") && activeNode != null 
                                    && activeNode.ReplaceInfo.SourceTag == "sub"))
                                {
                                    contentCollector.Append(reader.Value);
                                }
                            }
                            break;

                        default:
                            throw new ArgumentNullException();
                    }

                    reader.Read();
                }
            }

            result.HtmlData = bld.ToString();
            if (string.IsNullOrWhiteSpace(result.HtmlData))
                throw new ArgumentException();

            return result;
        }

        private string PrepareText(TagReplaceInfo info, string data, SoundCollector soundCollector, bool extractSounds)
        {
            string result;
            switch (info.ReplacementType)
            {
                case ReplacementType.LeaveOldTag:
                    result = string.Format("<{0}>{1}</{0}>", info.SourceTag, data);
                    break;
                case ReplacementType.ReplaceOldTag:
                    result = string.Format("<{0}{1}>{2}</{0}>", info.ReplacementTag,
                        string.IsNullOrEmpty(info.AdditionalData) ? null : " " + info.AdditionalData, 
                        data);
                    break;
                case ReplacementType.ReplaceImage:
                    if (string.IsNullOrEmpty(data))
                        throw new ArgumentException();

                    result = string.Format(info.AdditionalData, PrepareImagePath(data));
                    break;

                case ReplacementType.ReplaceLink:
                    if (string.IsNullOrEmpty(data))
                        throw new ArgumentException();

                    result = string.Format(info.AdditionalData, PrepareLink(data, false), data);
                    break;

                case ReplacementType.ReplaceSoundUK:
                case ReplacementType.ReplaceSoundUS:
                    if (string.IsNullOrEmpty(data))
                        throw new ArgumentException();

                    AudioButtonStyle buttonStyle = info.ReplacementType == ReplacementType.ReplaceSoundUK
                        ? (extractSounds ? AudioButtonStyle.BigUK : AudioButtonStyle.SmallUK)
                        : (extractSounds ? AudioButtonStyle.BigUS : AudioButtonStyle.SmallUS);

                    string soundKey = Path.GetFileNameWithoutExtension(data);
                    result = _buttonBuilder.BuildHtml(buttonStyle, soundKey);
                    if (soundCollector != null)
                    {
                        soundCollector.RegisterSound(soundKey, info.ReplacementType == ReplacementType.ReplaceSoundUK);
                    }
                    break;

                default:
                    throw new ArgumentException();
            }

            return result;
        }

        private void AddItem(List<ItemGroup> groups, DicItem item)
        {
            var group = groups.LastOrDefault();
            if (group == null || group.GroupType != item.ItemType)
            {
                // We can't have several groups of the same type
                if (groups.Any(x => x.GroupType == item.ItemType))
                {
                    throw new ArgumentException();
                }

                group = new ItemGroup { GroupType = item.ItemType, Items = new List<DicItem>() };
                groups.Add(group);
            }

            group.Items.Add(item);
        }

        private DicWord ProcessWord(XmlReader reader)
        {
            var word = new DicWord();

            reader.ScrollToStartTag(XmlBuilder.ElementKeywordName);
            word.Title = reader.ReadInnerXml();

            bool isFirstEntry = true;
            while (true)
            {
                if (isFirstEntry)
                {
                    reader.ScrollToStartTag(XmlBuilder.ElementEntryRoot, true);
                    isFirstEntry = false;
                }
                else
                {
                    if (!reader.ScrollToOptionalStartTag(XmlBuilder.ElementEntryRoot, true))
                    {
                        break;
                    }
                }

                // Optional entry number
                var entry = new DicEntry();
                if (reader.ScrollToOptionalStartTag(XmlBuilder.ElementEntryNumber))
                {
                    entry.EntryNumber = reader.ReadInnerXml();

                    reader.ScrollToStartTag(XmlBuilder.ElementEntryData);
                }
                else
                {
                    if (!(reader.NodeType == XmlNodeType.Element && reader.Name == XmlBuilder.ElementEntryData))
                        throw new ArgumentException();
                }

                // Main data
                entry.RawMainData = reader.ReadInnerXml(); // this will set position to the next tag or whitespace

                while (true)
                {
                    if (reader.NodeType == XmlNodeType.EndElement && reader.Name == XmlBuilder.ElementEntryRoot)
                    {
                        break;
                    }
                    reader.ScrollToNonWhitespace();
                    if (reader.NodeType == XmlNodeType.EndElement && reader.Name == XmlBuilder.ElementEntryRoot)
                    {
                        break;
                    }

                    if (reader.NodeType != XmlNodeType.Element)
                        throw new ArgumentException();

                    string elementName = reader.Name;

                    DicItem item = null;
                    switch (reader.Name)
                    {
                        case XmlBuilder.ElementWordForm:
                            item = new DicItem { ItemType = ItemType.WordForm };
                            break;

                        case XmlBuilder.ElementCollocation:
                            item  = new DicItem { ItemType =  ItemType.Collocation};
                            break;

                        case XmlBuilder.ElementComment:
                            string commentData = reader.ReadOuterXml();
                            if (entry.AllItems == null)
                            {
                                entry.RawMainData += commentData;
                            }
                            else
                            {
                                entry.AllItems[entry.AllItems.Count - 1].RawData += commentData;
                            }
                            break;

                        // Attach image to the previous node
                        case XmlBuilder.ElementCombImage:
                        case XmlBuilder.ElementImage:
                            string imageData;
                            if (reader.Name == XmlBuilder.ElementCombImage)
                            {
                                imageData = reader.ReadInnerXml();
                            }
                            else
                            {
                                imageData = reader.ReadOuterXml();
                            }

                            if (entry.AllItems == null)
                            {
                                entry.RawMainData += imageData;
                            }
                            else
                            {
                                entry.AllItems[entry.AllItems.Count - 1].RawData += imageData;
                            }
                            break;

                        default:
                            throw new ArgumentException();
                    }

                    if (item != null)
                    {
                        item.RawData = reader.ReadInnerXml();

                        if (entry.AllItems == null)
                        {
                            entry.AllItems = new List<DicItem>();
                        }
                        entry.AllItems.Add(item);
                    }
                }

                if (word.LPDEntries == null)
                {
                    word.LPDEntries = new List<DicEntry>();
                }
                word.LPDEntries.Add(entry);
            }

            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == XmlBuilder.ElementKeyword)
                return word;

            throw new ArgumentException();
        }

        private void GenerateTopList(string outputFolder)
        {
            var template = File.ReadAllText(
                Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    TopTemplateFilePath),
                Encoding.UTF8);

            var ranks = _usageBuilder.GetRanks();
            int previousRank = 0;
            foreach (var rank in ranks)
            {
                var bld = new StringBuilder();
                int ordinal = 0;
                foreach (var word in _usageBuilder.GetWords(rank).OrderBy(x => x.Keyword))
                {
                    ordinal++;
                    bld.AppendFormat(
@"      <div class=""topword_item""><span class=""topword_number"">{2}. </span><a class=""word_link"" href=""{0}"">{1}</a></div>
",
                    PrepareLink(word.Keyword, true), word.Keyword, ordinal);
                }

                string rankText = string.Format("{0}{1}",
                    (previousRank == 0 ? null : previousRank + " - "), rank);
                File.WriteAllText(
                    Path.Combine(outputFolder, string.Format("{0}.html", rank)),
                    string.Format(template, rankText, bld),
                    Encoding.UTF8);

                previousRank = rank;
            }
        }

        private List<string> PrepareWordNames(string[] contents)
        {
            if (contents == null || contents.Length == 0)
                return null;

            var names = new List<string>();
            string baseName = null;
            foreach (var rawItem in contents.Where(x => !string.IsNullOrEmpty(x))
                .SelectMany(x => x.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)))
            {
                var name = rawItem.Trim();
                if (string.IsNullOrEmpty(name))
                    continue;

                if (string.IsNullOrEmpty(baseName))
                {
                    baseName = name;
                    names.Add(name);
                    continue;
                }

                if (name.StartsWith("~"))
                {
                    name = name.Remove(0, 1);
                    if (name == "'") // Achilles'
                        continue;

                    int separatorIndex = baseName.LastIndexOf("|");
                    if (separatorIndex >= 0)
                    {
                        name = baseName.Substring(0, separatorIndex) + name;
                    }
                    else
                    {
                        name = baseName + name;
                    }
                }

                if (name.EndsWith("~"))
                {
                    name = name.Remove(name.Length - 1, 1);
                    if (name.Length == 1)
                    {
                        if (!string.Equals(name, baseName.Substring(0, 1), StringComparison.OrdinalIgnoreCase))
                            continue;

                        name = name + baseName.Remove(0, 1);
                    }
                    else
                    {
                        if (name.Contains("~")) // cabernet sauvignon (C~ S~)
                            continue;

                        int separatorIndex = baseName.IndexOf("|");
                        if (separatorIndex >= 0 && separatorIndex < baseName.Length - 1) // behavio|ristic, behavior|al
                        {
                            name = name + baseName.Substring(separatorIndex + 1);
                        }
                        else
                            continue; // Via Dolorosa
                    }
                }

                name = name.Replace("|", "");
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                names.Add(name);
            }

            return names;
        }

        // Test: Richmond-u₍ˌ₎pon-Thames, S₍ˌ₎VO language
        private List<string> PrepareCollocationNames(string[] contents)
        {
            if (contents == null || contents.Length == 0)
                return null;

            var names = new List<string>();
            foreach (var rawItem in contents.Where(x => !string.IsNullOrEmpty(x))
                .SelectMany(x => x.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)))
            {
                var name = rawItem.Trim();
                if (string.IsNullOrEmpty(name))
                    continue;

                // Ignore words with tilde or those starting with "-" because they will look meaningless without the context
                if (name.Contains("~") || name.StartsWith("-"))
                    continue;

                // Remove garbadge like: ˌ•• ˈ••
                name = name.Replace("•", string.Empty).Replace("ˌ", string.Empty).Replace("ˈ", string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                name = name.Replace("(", "").Replace(")", "");
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                names.Add(name);
            }

            return names;
        }

        private string PrepareJScriptSounds(Dictionary<string, string> sounds)
        {
            if (sounds == null || sounds.Count == 0)
                return null;

            var bld = new StringBuilder();
            bld.AppendLine("var pageAudio = {");

            bool isFirst = true;
            foreach (var sound in sounds)
            {
                if (!isFirst)
                {
                    bld.AppendLine(",");
                }
                isFirst = false;

                bld.AppendFormat(
@"      ""{0}"": ""{1}""",
                    sound.Key, sound.Value);
            }

            bld.Append(
@"
    };");

            return bld.ToString();
        }
    }
}
