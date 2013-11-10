using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Pronunciation.Parser
{
    class HtmlBuilder
    {
        private class WordGroup
        {
            public string Name;
            public List<DicWord> Words;
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
            public string SoundUK;
            public string SoundUS;
            public int TotalSounds;
        }

        private class WordUsageResult
        {
            public string Rank;
            public string PreviousWordHtml;
            public string NextWordHtml;
        }

        private readonly string _logFile;

        private static List<TagReplaceInfo> ReplaceMap = new List<TagReplaceInfo>();
        private static Regex _htmlRegex = new Regex("<.*?>", RegexOptions.Compiled);
        private static Regex _wordNameRegex = new Regex("[A-Za-z0-9 _'&+.-]", RegexOptions.Compiled);
        private static Regex _firstLetterRegex = new Regex("[A-Za-z0-9]", RegexOptions.Compiled);
        private static List<string> _forbiddenNames = new List<string>();

        private readonly WordUsageBuilder _usageBuilder;
        private readonly FileLoader _fileLoader;

        private const string TemplateFilePath = @"Html\Template.html";
        private const string TopTemplateFilePath = @"Html\TopTemplate.html";
        private const string RootPath = "../../";
        private const string ImagesPath = "Images/";
        private const string DicPath = "Dic/";
        private const string DicFolderName = "Dic";
        private const string IndexFileName = "Index.txt";

        private const string CaptionBigUk = "British";
        private const string CaptionBigUs = "American";
        private const string CaptionSmallUk = "BrE";
        private const string CaptionSmallUs = "AmE";

        private static Dictionary<char, string> _wordNameMap = new Dictionary<char, string>();

        static HtmlBuilder()
        {
            AddMap("strong", ReplacementType.LeaveOldTag, null, null);
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
            AddMap("stress_shift", ReplacementType.ReplaceOldTag, "span", "class=\"stress_shift\"", false);

            AddMap("pic", ReplacementType.ReplaceImage, "img", 
                "<img class=\"poll_image\" src=\"{0}\" />", false);
            AddMap("wlink", ReplacementType.ReplaceLink, "a", 
                "<a class=\"word_link\" href=\"{0}\">{1}</a>", false);
            AddMap("sound_uk", ReplacementType.ReplaceSoundUK, "button",
                "<button type=\"button\" class=\"audio_button audio_uk\" data-src=\"{0}\" raw-data=\"{1}\">{2}</button>",
                false);
            AddMap("sound_us", ReplacementType.ReplaceSoundUS, "button", 
                "<button type=\"button\" class=\"audio_button audio_us\" data-src=\"{0}\" raw-data=\"{1}\">{2}</button>",
                false);

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

        public HtmlBuilder(string logFile, FileLoader fileLoader)
        {
            _logFile = logFile;
            _fileLoader = fileLoader;
        }

        public HtmlBuilder(string logFile, FileLoader fileLoader, string wordUsageFile) : this(logFile, fileLoader)
        {
            _usageBuilder = new WordUsageBuilder(wordUsageFile);
        }

        private static void AddMap(string sourceTag, ReplacementType replacementType, string replacementTag, string additionalData)
        {
            AddMap(sourceTag, replacementType, replacementTag, additionalData, true);
        }
        private static void AddMap(string sourceTag, ReplacementType replacementType, string replacementTag, 
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

        public void ConvertToHtml(string sourceXml, string rootFolder, int maxFiles, bool isFakeMode)
        {
            var dicFolder = Path.Combine(rootFolder, DicFolderName);
            if (!Directory.Exists(dicFolder))
            {
                Directory.CreateDirectory(dicFolder);
            }

            File.AppendAllText(_logFile, "********* Starting conversion ***********\r\n\r\n");

            var words = ParseFile(sourceXml);

            if (_usageBuilder != null)
            {
                _usageBuilder.Initialize(words.Select(x => x.Keyword));
            }

            // Group by keyword
            var nameStats = new StringBuilder();
            var indexBuilder = new StringBuilder();
            var groups = new Dictionary<string, WordGroup>();
            foreach (var word in words)
            {
                if (word.Entries.Count <= 0)
                    throw new ArgumentException();

                string groupName = PrepareFileName(word.Keyword, nameStats);
                WordGroup group;
                if (!groups.TryGetValue(groupName, out group))
                {
                    group = new WordGroup { Name = groupName, Words = new List<DicWord>() };
                    groups.Add(groupName, group);
                }

                group.Words.Add(word);

                indexBuilder.AppendLine(string.Format("{0}\t{1}", word.Keyword, groupName));
            }
            File.AppendAllText(_logFile, "\r\nName stats:\r\n" + nameStats.ToString());
            File.WriteAllText(Path.Combine(rootFolder, IndexFileName), indexBuilder.ToString(), Encoding.UTF8);
            indexBuilder = null;

            // Load template
            var template = File.ReadAllText(
                Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    TemplateFilePath),
                Encoding.UTF8);

            int filesCount = 0;
            string outputFolder = null;
            string currentCacheFile = null;
            var titleStats = new StringBuilder();
            var soundStats = new StringBuilder();
            var letterStats = new StringBuilder();
            int wordsCount = 0;
            int letterGroups = 0;
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

                    if (currentCacheFile != null)
                    {
                        _fileLoader.FlushCache(currentCacheFile);
                    }
                    currentCacheFile = string.Format("{0}.txt", subFolder);
                    _fileLoader.LoadCache(currentCacheFile);

                    if (letterGroups > 0)
                    {
                        letterStats.AppendFormat(", groups: {0}\r\n", letterGroups);
                        Console.WriteLine(" ({0}) groups.", letterGroups);
                        
                        letterGroups = 0;
                    }

                    letterStats.AppendFormat("Letter: {0}", subFolder);
                    Console.Write("Processing " + subFolder);
                }

                var body = GenerateHtml(group, soundStats);

                string title = string.Join(" + ", group.Words.Select(x => x.Keyword));
                if (group.Words.Count > 1)
                {
                    titleStats.AppendFormat("Grouped words: {0}\r\n", title);
                }

                if (!isFakeMode)
                {
                    File.WriteAllText(
                        Path.Combine(outputFolder, string.Format("{0}.html", group.Name)),
                        string.Format(template, title, body),
                        Encoding.UTF8);
                }

                wordsCount += group.Words.Count;
                letterGroups++;

                filesCount++;
                if (maxFiles > 0 && filesCount >= maxFiles)
                    break;
            }

            _fileLoader.FlushCache(currentCacheFile);

            letterStats.AppendFormat(", groups: {0}\r\n", letterGroups);
            Console.WriteLine(" ({0}) groups.", letterGroups);
            Console.WriteLine("{0} words, {1} groups", wordsCount, groups.Count);

            File.AppendAllText(_logFile, "\r\nTitle stats:\r\n" + titleStats.ToString());
            File.AppendAllText(_logFile, "\r\nSound stats:\r\n" + soundStats.ToString());
            File.AppendAllText(_logFile, "\r\nLetter stats:\r\n" + letterStats.ToString());

            if (_usageBuilder != null)
            {
                GenerateTopList(rootFolder);
            }
            Console.WriteLine("Generated top words lists");

            File.AppendAllText(_logFile, "Ended conversion.\r\n\r\n");
        }

        private string GenerateHtml(WordGroup group, StringBuilder stats)
        {
            StringBuilder bldGroup = new StringBuilder();
            foreach (var word in group.Words)
            {
                WordUsageResult wordUsage = GetWordUsage(word.Keyword);

                string soundUK = null;
                string soundUS = null;
                var bldEntries = new StringBuilder();

                // Parse XML
                int ordinal = 0;
                foreach (var entry in word.Entries)
                {
                    ordinal++;
                    
                    // Parse main data
                    ParseResult result = ParseItemXml(entry.RawMainData, true);
                    if (result.TotalSounds != 2)
                    {
                        stats.AppendFormat("Number of sounds is [{0}] for [{1}] {2}\r\n", result.TotalSounds, word.Keyword, entry.EntryNumber);
                        result = ParseItemXml(entry.RawMainData, false);
                    }

                    bldEntries.Append(
@"      
        <div class=""entry"">
");

                    if (string.IsNullOrEmpty(entry.EntryNumber))
                    {
                        // Add sounds directly to the word name if entry number is missing
                        if (ordinal == 1)
                        {
                            soundUK = result.SoundUK;
                            soundUS = result.SoundUS;
                        }
                        else
                            throw new ArgumentException();
                    }
                    else
                    {
                        bldEntries.AppendFormat(
@"          <span class=""entry_number"">{0}</span>
            <span class=""entry_audio"">
                {1}
                {2}
            </span>
",
                            entry.EntryNumber, result.SoundUK, result.SoundUS);
                    }

                    bldEntries.AppendFormat(
@"          <div class=""entry_text"">{0}</div>
",
                            result.HtmlData);

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
                                bldEntries.Append(
@"          <div class=""forms"">
");
                            }
                            else
                            {
                                bldEntries.Append(
@"          <div class=""collocations"">
");
                            }

                            foreach (var item in itemGroup.Items)
                            {
                                result = ParseItemXml(item.RawData, false);
                                if (itemGroup.GroupType == ItemType.WordForm)
                                {
                                    bldEntries.AppendFormat(
@"              <div class=""form"">{0}</div>
",
                                        result.HtmlData);
                                }
                                else
                                {
                                    bldEntries.AppendFormat(
@"              <div class=""collocation"">{0}</div>
",
                                        result.HtmlData);
                                }
                            }

                            bldEntries.Append(
@"          </div>
");
                        }
                    }

                    bldEntries.Append(
@"      </div>");
                }

                bldGroup.AppendFormat(
@"  <div class=""word"">
        <span class=""word_name"">{0}</span>
",
                    word.Keyword);

                if (wordUsage != null)
                {
                    bldGroup.AppendFormat(
@"      <span class=""word_usage"">Usage TOP: <strong>{0}</strong>{1}{2}</span>
",
                        wordUsage.Rank, wordUsage.PreviousWordHtml, wordUsage.NextWordHtml);
                }

                if (!string.IsNullOrEmpty(soundUK) || !string.IsNullOrEmpty(soundUS))
                {
                    bldGroup.AppendFormat(
@"      <div class=""word_audio"">
            {0}
            {1}
        </div>
",
                        soundUK, soundUS);
                }

                bldGroup.AppendFormat(
@"{0}  
    </div>
",
                    bldEntries);
            }

            return bldGroup.ToString();
        }

        private string PrepareImagePath(string imageName)
        {
            return string.Format("{0}{1}{2}", RootPath, ImagesPath, imageName);
        }

        private string PrepareLink(string keyword, bool isRootedSource)
        {
            var fileName = PrepareFileName(keyword, null);
            return string.Format("{0}{1}{2}/{3}.html", 
                (isRootedSource ? null : RootPath), DicPath, fileName.Substring(0, 1), fileName);
        }

        private string PrepareFileName(string keyword, StringBuilder stat)
        {
            var text = _htmlRegex.Replace(keyword, string.Empty).Replace("&amp;", "&");
            if (text.EndsWith("..."))
            {
                text.Remove(text.Length - 3);
                if (stat != null)
                {
                    stat.AppendFormat("Ignored '...' at the end of [{0}] word\r\n", keyword);
                }
            }
            if (text.EndsWith("-"))
            {
                text.Remove(text.Length - 1);
                if (stat != null)
                {
                    stat.AppendFormat("Ignored '-' at the end of [{0}] word\r\n", keyword);
                }
            }
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

        private ParseResult ParseItemXml(string rawData, bool extractSounds)
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
                                var replacement = PrepareText(replace, null, false);
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
                            }
                            break;

                        case XmlNodeType.EndElement:
                            if (activeNode == null)
                                throw new ArgumentException();

                            if (activeNode.ReplaceInfo.SourceTag != reader.Name)
                                throw new ArgumentException();

                            var replacementText = PrepareText(activeNode.ReplaceInfo, activeNode.Data, extractSounds);
                            if (extractSounds)
                            {
                                if (activeNode.ReplaceInfo.ReplacementType == ReplacementType.ReplaceSoundUK)
                                {
                                    result.TotalSounds++;
                                    if (string.IsNullOrEmpty(result.SoundUK))
                                    {
                                        result.SoundUK = replacementText;
                                        replacementText = null;
                                    }
                                }
                                else if (activeNode.ReplaceInfo.ReplacementType == ReplacementType.ReplaceSoundUS)
                                {
                                    result.TotalSounds++;
                                    if (string.IsNullOrEmpty(result.SoundUS))
                                    {
                                        result.SoundUS = replacementText;
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

        private string PrepareText(TagReplaceInfo info, string data, bool extractSounds)
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
                        ? (extractSounds ? CaptionBigUk : CaptionSmallUk)
                        : (extractSounds ? CaptionBigUs : CaptionSmallUs);

                    string fileKey = Path.GetFileNameWithoutExtension(data);
                    result = string.Format(info.AdditionalData,
                        fileKey, _fileLoader.GetBase64Content(fileKey), lang);
                    break;

                default:
                    throw new ArgumentException();
            }

            return result;
        }

        private WordUsageResult GetWordUsage(string keyword)
        {
            if (_usageBuilder == null)
                return null;

            var info = _usageBuilder.GetUsage(keyword);
            if (info == null)
                return null;

            var result = new WordUsageResult
            {
                Rank = info.Rank.ToString()
            };

            if (info.PreviousWord != null)
            {
                result.PreviousWordHtml = string.Format("<a class=\"word_link previous_word\" href=\"{0}\">&lt; [{1}]</a>",
                    PrepareLink(info.PreviousWord.Keyword, false), info.PreviousWord.Keyword);
            }
            if (info.NextWord != null)
            {
                result.NextWordHtml = string.Format("<a class=\"word_link next_word\" href=\"{0}\">[{1}] &gt;</a>",
                    PrepareLink(info.NextWord.Keyword, false), info.NextWord.Keyword);
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

                if (word.Entries == null)
                {
                    word.Entries = new List<DicEntry>();
                }
                word.Entries.Add(entry);
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
    }
}
