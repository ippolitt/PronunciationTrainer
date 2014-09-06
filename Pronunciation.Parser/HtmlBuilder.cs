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

            public bool IsEmpty
            {
                get { return string.IsNullOrEmpty(SoundTextUK) && string.IsNullOrEmpty(SoundTextUS); }
            }
        }

        private class TagReplaceInfo
        {
            public string SourceTag;
            public ReplacementType ReplacementType;
            public string ReplacementTag;
            public string AdditionalData;
            public bool AllowChildTags;
        }

        private class CurrentTagInfo
        {
            public TagReplaceInfo ReplaceInfo;
            public string Data;
        }

        private enum ReplacementType
        {
            LeaveOldTag,
            ReplaceOldTag,
            ReplaceImage,
            ReplaceLink,
            ReplaceSoundUK,
            ReplaceSoundUS,
        }

        private class ParseResult
        {
            public string HtmlData;

            public string SoundTextUK;
            public string SoundTextUS;
        }

        private static Regex _htmlRegex = new Regex("<.*?>", RegexOptions.Compiled);
        private static Regex _wordNameRegex = new Regex("[A-Za-z0-9 _'&+.-]", RegexOptions.Compiled);
        private static Regex _firstLetterRegex = new Regex("[A-Za-z0-9]", RegexOptions.Compiled);

        private readonly List<TagReplaceInfo> ReplaceMap = new List<TagReplaceInfo>();
        private readonly List<string> _forbiddenNames = new List<string>();
        private readonly Dictionary<char, string> _wordNameMap = new Dictionary<char, string>();

        private readonly WordUsageBuilder _usageBuilder;
        private readonly Dictionary<int, int> _ranks;
        private IFileLoader _fileLoader;
        private readonly LDOCEHtmlBuilder _ldoce;
        private readonly DatabaseUploader _dbUploader;
        private IDATFileBuilder _audioDATBuilder;
        private IDATFileBuilder _htmlDATBuilder;
        private readonly string _logFile;
        private readonly string _binFolder;
        private readonly GenerationMode _generationMode;

        private const string TemplateFilePath = @"Html\FileTemplate.html";
        private const string TopTemplateFilePath = @"Html\TopTemplate.html";
        private const string RootPath = "../../";
        private const string ImagesPath = "Images/";
        private const string DicPath = "Dic/";
        private const string DicFolderName = "Dic";
        private const string IndexFileName = "Index.txt";

        public const string CaptionBigUK = "British";
        public const string CaptionBigUS = "American";
        public const string CaptionSmallUK = "BrE";
        public const string CaptionSmallUS = "AmE";

        private const string XmlElementStrong = "strong";

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

        private HtmlBuilder(GenerationMode generationMode)
        {
            _generationMode = generationMode;
            _binFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            AddMap(XmlElementStrong, ReplacementType.LeaveOldTag, null, null);
            AddMap("em", ReplacementType.LeaveOldTag, null, null);
            AddMap("sub", ReplacementType.LeaveOldTag, null, null);
            AddMap("sup", ReplacementType.LeaveOldTag, null, null);

            AddMap(XmlBuilder.ElementComment, ReplacementType.ReplaceOldTag, "div", "class=\"comment\"");
            AddMap("pron", ReplacementType.ReplaceOldTag, "span", "class=\"pron\"");
            AddMap("pron_us", ReplacementType.ReplaceOldTag, "span", "class=\"pron_us\"");
            AddMap("pron_us_alt", ReplacementType.ReplaceOldTag, "span", "class=\"pron_us_alt\"");
            AddMap("pron_other", ReplacementType.ReplaceOldTag, "span", "class=\"pron_other\"");
            AddMap("sample", ReplacementType.ReplaceOldTag, "span", "class=\"sample\"");
            AddMap("lang", ReplacementType.ReplaceOldTag, "span", "class=\"lang\"");
            AddMap("stress_up", ReplacementType.ReplaceOldTag, "span", "class=\"stress_up\"", false);
            AddMap("stress_low", ReplacementType.ReplaceOldTag, "span", "class=\"stress_low\"", false);
            AddMap("stress_low_optional", ReplacementType.ReplaceOldTag, "span", "class=\"stress_low_optional\"", false);
            AddMap("stress_shift", ReplacementType.ReplaceOldTag, "span", "class=\"stress_shift\"", false);

            AddMap("pic", ReplacementType.ReplaceImage, "img", 
                "<img class=\"poll_image\" src=\"{0}\" />", false);
            AddMap("wlink", ReplacementType.ReplaceLink, "a", 
                "<a class=\"word_link\" href=\"{0}\">{1}</a>", false);
            if (IsDatabaseMode)
            {
                AddMap("sound_uk", ReplacementType.ReplaceSoundUK, "button",
                    "<button type=\"button\" class=\"audio_button audio_uk\" data-src=\"{0}\">{1}</button>",
                    false);
                AddMap("sound_us", ReplacementType.ReplaceSoundUS, "button",
                    "<button type=\"button\" class=\"audio_button audio_us\" data-src=\"{0}\">{1}</button>",
                    false);
            }
            else
            {
                AddMap("sound_uk", ReplacementType.ReplaceSoundUK, "button",
                    "<button type=\"button\" class=\"audio_button audio_uk\" data-src=\"{0}\" raw-data=\"{1}\">{2}</button>",
                    false);
                AddMap("sound_us", ReplacementType.ReplaceSoundUS, "button",
                    "<button type=\"button\" class=\"audio_button audio_us\" data-src=\"{0}\" raw-data=\"{1}\">{2}</button>",
                    false);
            }

            _wordNameMap.Add('/', "-");
            _wordNameMap.Add('à', "a");
            _wordNameMap.Add('á', "a");
            _wordNameMap.Add('å', "a");
            _wordNameMap.Add('â', "a");
            _wordNameMap.Add('ä', "a");
            _wordNameMap.Add('ã', "a");
            _wordNameMap.Add('é', "e");
            _wordNameMap.Add('è', "e");
            _wordNameMap.Add('ë', "e");
            _wordNameMap.Add('ê', "e");
            _wordNameMap.Add('É', "E");
            _wordNameMap.Add('Æ', "ae");
            _wordNameMap.Add('ó', "o");
            _wordNameMap.Add('ö', "o");
            _wordNameMap.Add('ô', "o"); 
            _wordNameMap.Add('ø', "o"); 
            _wordNameMap.Add('Ó', "O");
            _wordNameMap.Add('Ö', "O");
            _wordNameMap.Add('ç', "c");
            _wordNameMap.Add('č', "c");
            _wordNameMap.Add('Č', "C");
            _wordNameMap.Add('š', "s");
            _wordNameMap.Add('ü', "u");
            _wordNameMap.Add('û', "u");
            _wordNameMap.Add('ů', "u"); 
            _wordNameMap.Add('ñ', "n");
            _wordNameMap.Add('í', "i");
            _wordNameMap.Add('î', "i");

            // These names are borbidden in Windows OS
            _forbiddenNames.AddRange(
                "CON, PRN, AUX, NUL, COM1, COM2, COM3, COM4, COM5, COM6, COM7, COM8, COM9, LPT1, LPT2, LPT3, LPT4, LPT5, LPT6, LPT7, LPT8, LPT9"
                .ToLower().Split(new []{ ',', ' '}));
        }

        public HtmlBuilder(GenerationMode generationMode, string connectionString, IDATFileBuilder audioDATBuilder,
            IDATFileBuilder htmlDATBuilder, IFileLoader fileLoader, LDOCEHtmlBuilder ldoce, string wordUsageFile, string logFile)
            : this(generationMode)
        {
            _logFile = logFile;
            _fileLoader = fileLoader;
            _ldoce = ldoce;
            _audioDATBuilder = audioDATBuilder;
            _htmlDATBuilder = htmlDATBuilder;
            if (IsDatabaseMode)
            {
                _dbUploader = new DatabaseUploader(connectionString);
            }
            _usageBuilder = new WordUsageBuilder(wordUsageFile);
            _ranks = PrepareRanks(_usageBuilder.GetRanks());
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

        private void AddMap(string sourceTag, ReplacementType replacementType, string replacementTag, string additionalData)
        {
            AddMap(sourceTag, replacementType, replacementTag, additionalData, true);
        }
        private void AddMap(string sourceTag, ReplacementType replacementType, string replacementTag, 
            string additionalData, bool allowChildTags)
        {
            ReplaceMap.Add(new TagReplaceInfo 
            { 
                SourceTag = sourceTag,
                ReplacementType = replacementType,
                ReplacementTag = replacementTag,
                AdditionalData = additionalData,
                AllowChildTags = allowChildTags
            });
        }

        public void ConvertToHtml(string sourceXml, string htmlFolder, int maxWords, bool isFakeMode, bool deleteExtraWords)
        {
            File.AppendAllText(_logFile, string.Format("********* Starting conversion {0} ***********\r\n\r\n", DateTime.Now));

            List<DicWord> words = ParseFile(sourceXml);
            if (_ldoce != null)
            {
                AddLDOCEEntries(words);
            }
            _usageBuilder.Initialize(words.Select(x => x.Keyword));

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

            if (GenerateTopWordsList)
            {
                GenerateTopList(htmlFolder);
                Console.WriteLine("Generated top words lists");
            }

            File.AppendAllText(_logFile, "Ended conversion.\r\n\r\n");
        }

        private void AddLDOCEEntries(List<DicWord> words)
        {
            var keywords = new List<string>();
            IFileLoader fileLoader = _fileLoader;
            IDATFileBuilder audioBuilder = _audioDATBuilder;
            IDATFileBuilder htmlBuilder = _htmlDATBuilder;
            
            // We use mock instead of the actual file loader to speed up HTML generation inside "AddCollocations" method
            _fileLoader = new FileLoaderMock();
            _audioDATBuilder = new DATBuilderMock();
            _htmlDATBuilder = new DATBuilderMock();
            try
            {
                foreach (var word in words)
                {
                    word.LDOCEEntry = _ldoce.GetEntry(word.Keyword);
                    keywords.Add(word.Keyword);
                    foreach (DicEntry entry in word.LPDEntries)
                    {
                        AddCollocations(entry, keywords);
                    }
                }
            }
            finally
            {
                _fileLoader = fileLoader;
                _audioDATBuilder = audioBuilder;
                _htmlDATBuilder = htmlBuilder;
            }

            var extraEntries = _ldoce.GetExtraEntries(keywords);
            words.AddRange(extraEntries.Select(x => new DicWord { Keyword = x.Keyword, LDOCEEntry = x, IsLDOCEEntry = true }));
        }

        private int AddCollocations(DicEntry entry, List<string> collocations)
        {
            if (entry.AllItems == null || entry.AllItems.Count <= 0)
                return 0;

            int collocationsCount = 0;
            foreach (var item in entry.AllItems.Where(x => x.ItemType == ItemType.Collocation))
            {
                var contentCollector = new ContentCollector(XmlElementStrong, true);
                ParseItemXml(item.RawData, false, contentCollector, null);
                string[] names = ParseCollocationsContent(contentCollector.GetContent());
                if (names != null)
                {
                    collocations.AddRange(names);
                    collocationsCount += names.Length;
                }
            }

            return collocationsCount;
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
                foreach (var group in groups.Values.OrderBy(x => x.Name, StringComparer.Ordinal))
                {
                    var letter = group.Name.Substring(0, 1);
                    if (currentLetter != letter)
                    {
                        _fileLoader.FlushCache();
                        _fileLoader.LoadCache(letter);

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
                        if (!isFakeMode)
                        {
                            var htmlIndex = _htmlDATBuilder.AppendEntity(
                                description.Text, 
                                Encoding.UTF8.GetBytes(pageBuilder.ToString()));
                            _dbUploader.StoreWord(description, word.IsLDOCEEntry, htmlIndex.BuildKey());
                        }

                        letterWords++;
                        wordsCount++;
                    }

                    if (maxWords > 0 && wordsCount >= maxWords)
                        break;
                }

                Console.WriteLine(" ({0}) words.", letterWords);
                Console.WriteLine("Total: {0} words", wordsCount);

                if (!isFakeMode && deleteExtraWords)
                {
                    int deleteCount = _dbUploader.DeleteExtraWords();
                    Console.WriteLine("Deleted '{0}' extra words", deleteCount);
                }

                if (_dbUploader.DbStats != null)
                {
                    File.AppendAllText(_logFile, "\r\nDatabase upload stats:\r\n" + _dbUploader.DbStats.ToString());
                }
            }
            finally
            {
                _dbUploader.Dispose();
                _htmlDATBuilder.Flush();
                _audioDATBuilder.Flush();
            }

            _fileLoader.FlushCache();
            _fileLoader.ClearCache();
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

                    _fileLoader.FlushCache();
                    _fileLoader.LoadCache(subFolder);

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
                foreach (var word in group.Words)
                {
                    WordDescription description = GenerateHtml(word, pageBuilder, soundStats);
                    int? usageRank = description.UsageInfo == null ? (int?)null : description.UsageInfo.CombinedRank;
                    indexBuilder.AppendLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}",
                        description.Text, group.Name, 0, usageRank,
                        description.SoundKeyUK, description.SoundKeyUS, word.IsLDOCEEntry ? 1 : 0));

                    if (description.Collocations != null)
                    {
                        foreach (var collocation in description.Collocations)
                        {
                            indexBuilder.AppendLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}",
                                collocation.Text, group.Name, 1, null,
                                collocation.SoundKeyUK, collocation.SoundKeyUS, 0));
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
                        string.Format(template, title, pageBuilder),
                        Encoding.UTF8);
                }

                letterGroups++;
                wordsCount += group.Words.Count; 
                if (maxWords > 0 && wordsCount >= maxWords)
                    break;
            }

            _fileLoader.FlushCache();
            _fileLoader.ClearCache();

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
                if (!word.IsLDOCEEntry && word.LPDEntries.Count <= 0)
                    throw new ArgumentException();

                string groupName = PrepareFileName(word.Keyword, nameStats);
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
                    ldoceHtml = _ldoce.GenerateFragmentHtml(word.LDOCEEntry);
                }
            }

            pageBuilder.AppendFormat(
@"  <div class=""word"">
    <span class=""word_name"">{0}</span>
",
                word.Keyword);

            if (wordDescription.UsageInfo != null)
            {
                pageBuilder.Append(GenerateUsageInfoHtml(wordDescription.UsageInfo));
            }

            if (wordAudio != null && !wordAudio.IsEmpty)
            {
                pageBuilder.AppendFormat(
@"      <div class=""word_audio"">
        {0}
        {1}
    </div>
",
                    wordAudio.SoundTextUK, wordAudio.SoundTextUS);
            }

            pageBuilder.AppendFormat(
@"{0}{1}
</div>  
",
                lpdHtml, ldoceHtml);

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
                int previousRank = _ranks[rank.CombinedRank];
                return string.Format(
@"      <span class=""word_usage"">Usage rank: top {0}{1}</span>
",
                    previousRank == 0 ? null : string.Format(@"<span class=""word_usage_from"">{0}</span>", previousRank),
                    string.Format(@"<span class=""word_usage_to"">{0}</span>", rank.CombinedRank));
            }
        }

        private string GenerateLPDHtml(string keyword, List<DicEntry> entries, WordDescription wordDescription,
            StringBuilder soundStats, out WordAudio wordAudio)
        {
            wordAudio = null;
            var bld = new StringBuilder();
            bool isWordAudioSet = false;
            int ordinal = 0;
            foreach (var entry in entries)
            {
                ordinal++;

                // Parse main data
                var entrySoundCollector = new SoundCollector(keyword);
                ParseResult entryResult = ParseItemXml(entry.RawMainData, true, null, entrySoundCollector);
                wordDescription.Sounds.AddRange(entrySoundCollector.Sounds);

                // If word entry has exactly two sounds (UK & US) then these sounds are considered as entry audio 
                if (entrySoundCollector.HasUKSound && entrySoundCollector.HasUSSound
                    && entrySoundCollector.Sounds.Count == 2)
                {
                    // Consider audio of the first entry as the word audio
                    if (!isWordAudioSet)
                    {
                        wordDescription.SoundKeyUK = entrySoundCollector.MainSoundUK;
                        wordDescription.SoundKeyUS = entrySoundCollector.MainSoundUS;
                        isWordAudioSet = true;
                    }
                }
                else
                {
                    // Otherwise we rebuild output html without extracting the audio
                    entryResult = ParseItemXml(entry.RawMainData, false, null, null);

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
                }

                bld.Append(
@"      
    <div class=""entry"">
");

                if (string.IsNullOrEmpty(entry.EntryNumber))
                {
                    // Add sounds directly to the word name if entry number is missing
                    if (ordinal == 1)
                    {
                        wordAudio = new WordAudio { SoundTextUK = entryResult.SoundTextUK, SoundTextUS = entryResult.SoundTextUS };
                    }
                    else
                        throw new ArgumentException();
                }
                else
                {
                    bld.AppendFormat(
@"          <span class=""entry_number"">{0}</span>
",
                        entry.EntryNumber);

                    if (!string.IsNullOrEmpty(entryResult.SoundTextUK) || !string.IsNullOrEmpty(entryResult.SoundTextUS))
                    {
                        bld.AppendFormat(
@"      <span class=""entry_audio"">
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
                            ContentCollector contentCollector = null;
                            if (itemGroup.GroupType == ItemType.Collocation)
                            {
                                contentCollector = new ContentCollector(XmlElementStrong, true);
                            }

                            var itemSoundCollector = new SoundCollector(null);
                            ParseResult itemResult = ParseItemXml(item.RawData, false, contentCollector, itemSoundCollector);
                            if (itemGroup.GroupType == ItemType.Collocation)
                            {
                                var contentItems = ParseCollocationsContent(contentCollector.GetContent());
                                if (contentItems != null && contentItems.Length > 0)
                                {
                                    if (wordDescription.Collocations == null)
                                    {
                                        wordDescription.Collocations = new List<CollocationDescription>();
                                    }

                                    wordDescription.Collocations.AddRange(contentItems.Select(x => new CollocationDescription
                                    {
                                        Text = x,
                                        SoundKeyUK = itemSoundCollector.MainSoundUK,
                                        SoundKeyUS = itemSoundCollector.MainSoundUS
                                    }));

                                    itemSoundCollector.RegisterSoundText(contentItems[0]);
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
                return string.Format("javascript:void(loadPage('{0}'))", keyword);
            }
            else
            {
                var fileName = PrepareFileName(keyword, null);
                return string.Format("{0}{1}{2}/{3}.html",
                    (isRootedSource ? null : RootPath), DicPath, fileName.Substring(0, 1), fileName);
            }
        }

        private string PrepareFileName(string keyword, StringBuilder stat)
        {
            var text = _htmlRegex.Replace(keyword, string.Empty).Replace("&amp;", "&");
            // TODO: after migration to DB uncomment this code for generating html files ???
            //if (text.EndsWith("..."))
            //{
            //    text = text.Remove(text.Length - 3);
            //    if (stat != null)
            //    {
            //        stat.AppendFormat("Ignored '...' at the end of [{0}] word\r\n", keyword);
            //    }
            //}
            //if (text.EndsWith("-"))
            //{
            //    text = text.Remove(text.Length - 1);
            //    if (stat != null)
            //    {
            //        stat.AppendFormat("Ignored '-' at the end of [{0}] word\r\n", keyword);
            //    }
            //}

            var bld = new StringBuilder();
            Regex regex; 
            foreach (var ch in text)
            {
                if (bld.Length == 0)
                {
                    regex = _firstLetterRegex;
                }
                else
                {
                    regex = _wordNameRegex;
                }

                if (regex.IsMatch(ch.ToString()))
                {
                    bld.Append(ch);
                }
                else
                {
                    string replacement;
                    if (_wordNameMap.TryGetValue(ch, out replacement))
                    {
                        if (regex.IsMatch(replacement))
                        {
                            bld.Append(replacement);
                        }
                        else
                        {
                            if (stat != null)
                            {
                                stat.AppendFormat("Ignored [{0}] and replacement [{1}] in [{2}]\r\n", ch, replacement, keyword);
                            }
                        }
                    }
                    else
                    {
                        if (stat != null)
                        {
                            stat.AppendFormat("Ignored [{0}] in [{1}]\r\n", ch, keyword);
                        }
                    }
                }
            }

            var result = bld.ToString().ToLower();

            if (_forbiddenNames.Contains(result))
            {
                if (stat != null)
                {
                    stat.AppendFormat("Using [_{0}] instead of [{0}] (forbidden OS name)\r\n", result);
                }
                return "_" + result;
            }

            return result;
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

        private ParseResult ParseItemXml(string rawData, bool extractSounds, ContentCollector contentCollector, 
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

                            var replace = ReplaceMap.Single(x => x.SourceTag == reader.Name);
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
                                contentCollector.Append(reader.Value);
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

                    string lang = info.ReplacementType == ReplacementType.ReplaceSoundUK
                        ? (extractSounds ? CaptionBigUK : CaptionSmallUK)
                        : (extractSounds ? CaptionBigUS : CaptionSmallUS);

                    string fileKey = Path.GetFileNameWithoutExtension(data);
                    string soundIndex = null;
                    if (IsDatabaseMode)
                    {
                        byte[] audioData = _fileLoader.GetRawData(fileKey);
                        soundIndex = _audioDATBuilder.AppendEntity(fileKey, audioData).BuildKey();
                        result = string.Format(info.AdditionalData, fileKey, lang);
                    }
                    else
                    {
                        result = string.Format(info.AdditionalData,
                            fileKey, _fileLoader.GetBase64Content(fileKey), lang);
                    }

                    if (soundCollector != null)
                    {
                        soundCollector.RegisterSound(fileKey, soundIndex, info.ReplacementType == ReplacementType.ReplaceSoundUK);
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
            word.Keyword = reader.ReadInnerXml(); // Some keywords contain HTML tags in them (e.g. H20)

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

        // Test: Richmond-u₍ˌ₎pon-Thames, S₍ˌ₎VO language
        // Test how ₍ˌ₎ looks
        private string[] ParseCollocationsContent(string rawContent)
        {
            if (string.IsNullOrWhiteSpace(rawContent))
                return null;

            List<string> contentItems = new List<string>();
            foreach (var rawItem in rawContent.Trim().Split(new [] {','}, StringSplitOptions.RemoveEmptyEntries))
            {
                if (string.IsNullOrWhiteSpace(rawItem))
                    continue;

                // Ignore words with tilde or those starting with "-" because they will look meaningless without the context
                var contentItem = rawItem.Trim();
                if (contentItem.Contains("~") || contentItem.StartsWith("-"))
                    continue;

                // Remove garbadge like: ˌ•• ˈ••
                contentItem = contentItem.Replace("•", string.Empty).Replace("ˌ", string.Empty).Replace("ˈ", string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(contentItem))
                    continue;

                // Some words contain "₍₎" with some garbidge symbols in the beginning ("South Africa", "trade union")
                var index = contentItem.IndexOf("₎");
                if (index >= 0)
                {
                    contentItem = contentItem.Remove(0, index + 1).Trim();
                }
                if (string.IsNullOrWhiteSpace(contentItem))
                    continue;

                contentItems.Add(contentItem);
            }

            return contentItems.Count == 0 ? null : contentItems.ToArray();
        }
    }
}
