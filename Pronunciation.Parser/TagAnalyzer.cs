using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Pronunciation.Parser
{
    public class TagInfo
    {
        public EntryType Entry;
        public int TagsCount;
        public string Example;
    }

    // Compare:
    // "-ed"
    class TagAnalyzer
    {
        private class ReplaceInfo
        {
            public bool ApplyToAll;
            public EntryType Filter;
            public string OpenTag;
            public string CloseTag;
            public string OpenTagReplacement;
            public string CloseTagReplacement;
            public bool AllowSecondAtempt;
        }

        private class TagEntry
        {
            public ReplaceInfo Item;
            public string Data;
            public int StartIndex;  
        }

        public Dictionary<string, List<TagInfo>> Tags = new Dictionary<string, List<TagInfo>>();

        private static List<ReplaceInfo> ReplaceMap = new List<ReplaceInfo>();
        public StringBuilder BldStats = new StringBuilder();
        private const string PronTag = "pron";
        private const string PronUsAltTag = "pron_us_alt";
        private const string StrongTag = "strong";
        private const string EntryNameTag = "entry_name";
        private const string CollocationNameTag = "col_name";
        private const string WordFormNameTag = "form_name";

        private string _activeWord;

        //We search strings like this: "<pron>pɔːz</pron> pɑːz"
        private Regex _regMissedAmE = new Regex(string.Format(@"(<{0}>.*ɔː.*?</{0}>[^<]* )([^\s<]*ɑː[^\s<]*)\z", PronTag), RegexOptions.Compiled);

        static TagAnalyzer()
        {
            AddMap("[b]", "[/b]", StrongTag);
            // do it only for collocations to separate phrase stress from word stress
            AddMap("[c blue]ˈ[/c]", null, "<stress_up/>", null, false, EntryType.Collocation);
            AddMap("[c blue] ˈ[/c]", null, " <stress_up/>", null, false, EntryType.Collocation); // the space before <stress_up/> is required
            AddMap("[c blue]     ˈ[/c]", null, "<stress_up/>", null, false, EntryType.Collocation); // used in "spending money"
            AddMap("[c blue]ˌ[/c]", null, "<stress_low/>", null, false, EntryType.Collocation);
            AddMap("[c blue] ˌ[/c]", null, "<stress_low/>", null, false, EntryType.Collocation); // no space before <stress_low/>

            string optionalLowStress = "<stress_low_optional><sub>(</sub><stress_low/><sub>)</sub></stress_low_optional>";
            AddMap("[c blue]₍ˌ₎[/c]", null, optionalLowStress, null, false, EntryType.Collocation);
            AddMap("[c blue]̪₍ˌ₎[/c]", null, optionalLowStress, null, false, EntryType.Collocation); // South Africa
            AddMap("[c blue]̯₍ˌ₎[/c]", null, optionalLowStress, null, false, EntryType.Collocation); // Trade union
            AddMap("[c blue]₍ˌ₎ˈ[/c]", null, optionalLowStress + "<stress_up/>", null, false, EntryType.Collocation); // cwm -> Rhondda
            AddMap("[c blue]◂[/c]", null, "<stress_shift/>", null, false);
            AddMap("[c blue]", "[/c]", null); // remove the tags for all other cases

            AddMap("[c darkmagenta]", "[/c]", "sample"); // Sample or another word with the same pronunciation
            AddMap("[c green][i]", "[/i][/c]", "lang"); // name of the other language
            AddMap(@"\[[c mediumblue]", @"[/c]\]", "<pron_other>", "</pron_other>", true); // transcription in other language
            AddMap(@"\[", @"\]", "[", "]", false); // special case: see Aragon
            AddMap(@"[c mediumblue]\[", @"\][/c]", "<pron_other>", "</pron_other>", false); // transcription in other language (rare case)
            AddMap("[c mediumblue]", "[/c]", PronTag); // recommended pronunciation
            AddMap("[i]", "[/i]", "em");
            AddMap(@"[p]AmE[/p]\ [c mediumblue]", "[/c]", "pron_us");
            AddMap("[p]AmE[/p] [s]", "[/s]", "sound_us");
            AddMap("[p]BrE[/p] [s]", "[/s]", "sound_uk");
            AddMap(@"[p]§[/p]\", null, "§", null, false);
            AddMap("[s]", "[/s]", XmlBuilder.ElementImage, false, EntryType.ImageComb); // replace [s] pair within combined image to the "pic" tag
            AddMap("[sub]", "[/sub]", "sub");
            AddMap("{[sub]}", "{[/sub]}", "sub", false, EntryType.Keyword);
            AddMap("[sup]", "[/sup]", "sup");
            AddMap(@"\~", null, "~", null, false);
            AddMap("↑<<", ">>", "wlink");
        }

        private static void AddMap(string openTag, string closeTag, string xmlTag)
        {
            AddMap(openTag, closeTag, xmlTag, true, EntryType.MainEntry);
        }

        private static void AddMap(string openTag, string closeTag, string xmlTag, bool applyToAll, EntryType filter)
        {
            ReplaceMap.Add(new ReplaceInfo
            {
                ApplyToAll = applyToAll,
                Filter = filter,
                OpenTag = openTag,
                CloseTag = closeTag,
                OpenTagReplacement = string.IsNullOrEmpty(xmlTag) ? null : string.Format("<{0}>", xmlTag),
                CloseTagReplacement = string.IsNullOrEmpty(xmlTag) ? null : string.Format("</{0}>", xmlTag)
            });
        }

        private static void AddMap(string openTag, string closeTag, string openTagReplacement, string closeTagReplacement, 
            bool allowSecondAttempt)
        {
            ReplaceMap.Add(new ReplaceInfo
            {
                ApplyToAll = true,
                OpenTag = openTag,
                CloseTag = closeTag,
                OpenTagReplacement = openTagReplacement,
                CloseTagReplacement = closeTagReplacement,
                AllowSecondAtempt = allowSecondAttempt
            });
        }

        private static void AddMap(string openTag, string closeTag, string openTagReplacement, string closeTagReplacement,
            bool allowSecondAttempt, EntryType filter)
        {
            ReplaceMap.Add(new ReplaceInfo
            {
                ApplyToAll = false,
                Filter = filter,
                OpenTag = openTag,
                CloseTag = closeTag,
                OpenTagReplacement = openTagReplacement,
                CloseTagReplacement = closeTagReplacement,
                AllowSecondAtempt = allowSecondAttempt
            });
        }

        public void AnalyzeText(EntryType entry, string text, string word)
        {
            string s = null;
            bool isOpen = false;
            bool isForeignOpen = false;
            char previousChar = ' ';
            foreach (var ch in text)
            {
                if (ch == '[')
                {
                    if (isOpen)
                        throw new Exception("Tag is already open!");

                    if (previousChar == '\\')
                    {
                        if (isForeignOpen)
                            throw new Exception("Foreign Tag is already open!");

                        isForeignOpen = true;
                    }
                    else
                    {
                        s = null;
                        isOpen = true;
                    }
                }
                else if (ch == ']')
                {
                    if (previousChar == '\\')
                    {
                        if (!isForeignOpen)
                            throw new Exception("Foreign Tag is not open!");

                        RegisterTag(entry, @"\[...\] (foreign words)", word);
                        isForeignOpen = false;
                    }
                    else
                    {
                        if (!isOpen)
                            throw new Exception("Tag is not open!");

                        RegisterTag(entry, s, word);
                        isOpen = false;
                    }
                }
                else if (isOpen)
                {
                    s += ch;
                }

                previousChar = ch;
            }
        }

        public string ConvertText(EntryType entry, string text, string word)
        {
            _activeWord = word;

            var tokens = ReplaceMap.Where(x => x.ApplyToAll || x.Filter == entry).ToArray();

            string token = null;
            bool isActiveToken = false;
            ReplaceInfo pendingToken = null;
            TagEntry activeTag = null;
            int pendingIndex = 0;
            var bld = new StringBuilder();
            var activeTokens = new Stack<TagEntry>();
            bool isSecondAttempt = false;
            for (int i = 0; i < text.Length; i++)
            {
                var ch = text[i];

                // Every '[' starts a token (unless it's already started)
                if (ch == '[' || ch == '\\' || ch == '{' || ch == '↑' || ch == '>')
                {
                    if (!isActiveToken)
                    {
                        isActiveToken = true;
                    }
                }

                if (isActiveToken)
                {
                    token += ch;

                    // Check exact match by opening token
                    var tokenInfo = tokens.FirstOrDefault(x => x.OpenTag == token 
                        && !(isSecondAttempt && x.AllowSecondAtempt));
                    if (tokenInfo != null)
                    {
                        // Check if more specific match is possible
                        if (tokens.Any(x => x.OpenTag.StartsWith(token, StringComparison.Ordinal) && x.OpenTag != token
                            && !(isSecondAttempt && x.AllowSecondAtempt)))
                        {
                            pendingToken = tokenInfo;
                            pendingIndex = i;
                            continue;
                        }
                    }

                    if (tokenInfo == null && pendingToken != null)
                    {
                        // Check if more specific token still match
                        if (tokens.Any(x => x.OpenTag.StartsWith(token, StringComparison.Ordinal) && !(isSecondAttempt && x.AllowSecondAtempt)))
                        {
                            continue;
                        }

                        // More specific match failed - revert index to the end of the previous token
                        i = pendingIndex;
                        ch = text[i];
                        tokenInfo = pendingToken;
                        token = tokenInfo.OpenTag;

                        pendingToken = null;
                        pendingIndex = 0;
                    }

                    if (tokenInfo != null)
                    {
                        isSecondAttempt = false;

                        // More specific match succeeded
                        if (pendingToken != null)
                        {
                            pendingToken = null;
                            pendingIndex = 0;
                        }

                        if (string.IsNullOrEmpty(tokenInfo.CloseTag))
                        {
                            if (!string.IsNullOrEmpty(tokenInfo.CloseTagReplacement))
                            {
                                throw new Exception("Can't specify close tag replacement without specifying close tag!");
                            }

                            if (activeTag != null)
                            {
                                activeTag.Data += tokenInfo.OpenTagReplacement;
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(tokenInfo.OpenTagReplacement))
                                {
                                    bld.Append(tokenInfo.OpenTagReplacement);
                                }
                            }
                        }
                        else
                        {
                            activeTag = new TagEntry 
                            { 
                                Item = tokenInfo,
                                StartIndex = i - tokenInfo.OpenTag.Length + 1
                            };
                            if (!string.IsNullOrEmpty(activeTag.Item.OpenTagReplacement))
                            {
                                activeTag.Data += activeTag.Item.OpenTagReplacement;
                            }

                            activeTokens.Push(activeTag);
                        }
                        isActiveToken = false;
                        token = null;
                        continue;
                    }

                    // Check exact match by closing tokens
                    if (activeTag != null)
                    {
                        if (activeTag.Item.CloseTag == token)
                        {
                            if (activeTag.Item.Filter == EntryType.ImageComb)
                            {
                                if (!activeTag.Data.EndsWith(".png"))
                                    throw new Exception("Attempt to treat tag as image!");
                            }

                            if (!string.IsNullOrEmpty(activeTag.Item.CloseTagReplacement))
                            {
                                activeTag.Data += activeTag.Item.CloseTagReplacement;
                            }

                            activeTokens.Pop();
                            if (activeTokens.Count > 0)
                            {
                                var previousTag = activeTokens.Peek();
                                previousTag.Data += activeTag.Data;
                                activeTag = previousTag;
                            }
                            else
                            {
                                bld.Append(activeTag.Data);
                                activeTag = null;
                            }

                            isActiveToken = false;
                            token = null;
                            continue;
                        }
                    }

                    // Check "starts with" opening token
                    if (tokens.Any(x => x.OpenTag.StartsWith(token, StringComparison.Ordinal)))
                    {
                        continue;
                    }

                    // Check "starts with" closing token
                    if (activeTag != null && activeTag.Item.CloseTag.StartsWith(token, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    // Remove the last tag, rewind backward and try one more time (but without specific matches)
                    if (activeTag != null && activeTag.Item.AllowSecondAtempt)
                    {
                        i = activeTag.StartIndex - 1;
                        activeTokens.Pop();
                        activeTag = activeTokens.Count > 0 ? activeTokens.Peek() : null;
                        isActiveToken = false;
                        token = null;
                        isSecondAttempt = true;
                        continue;
                    }

                    throw new Exception(string.Format("Unknown token '{0}'!", token));
                }
                else
                {
                    if (activeTag == null)
                    {
                        bld.Append(ValidateSymbol(ch));
                    }
                    else
                    {
                        activeTag.Data += ValidateSymbol(ch);
                    }
                }
            }

            if (pendingToken != null)
                throw new ArgumentException();

            string result = bld.ToString().Replace(" ◂", "<stress_shift/>");
            result = _regMissedAmE.Replace(result, ReplaceEvaluator);
            if (entry == EntryType.MainEntry)
            {
                result = ReplaceItemName(result, EntryNameTag, true);
            }
            else if (entry == EntryType.Collocation)
            {
                result = ReplaceItemName(result, CollocationNameTag, false);
            }
            else if (entry == EntryType.WordForm)
            {
                result = ReplaceItemName(result, WordFormNameTag, false);
            }

            return result;
        }

        private string ReplaceEvaluator(Match match)
        {
            var result = string.Format("{0}<{2}>{1}</{2}>", match.Groups[1].Value, match.Groups[2].Value, PronUsAltTag);
            BldStats.AppendFormat("{0}: {1}\r\n", _activeWord, match.Groups[2].Value);

            return result;
        }

        private string ValidateSymbol(char ch)
        {
            return ch.ToString().Replace("&", "&amp;");
        }

        private string ReplaceItemName(string text, string replacementTag, bool firstOccurenceOnly)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var reader = new TagReader(text);
            var bld = new StringBuilder();
            string openTag = string.Format("<{0}>", StrongTag);
            string closeTag = string.Format("</{0}>", StrongTag);
            while (reader.LoadTagContent(openTag, closeTag, false))
            {
                text = text.Replace(
                    string.Format("{0}{1}{2}", openTag, reader.Content, closeTag),
                    string.Format("<{0}>{1}</{0}>", replacementTag, reader.Content));
                if (firstOccurenceOnly)
                    break;

                // "<em> —see"
                // "<em> —or see"
            }

            return text;
        }

        private void RegisterTag(EntryType entry, string tag, string word)
        {
            if (string.IsNullOrWhiteSpace(tag))
                throw new ArgumentNullException();

            List<TagInfo> items;
            if (!Tags.TryGetValue(tag, out items))
            {
                items = new List<TagInfo>();
                Tags.Add(tag, items);
            }

            var item = items.SingleOrDefault(x => x.Entry == entry);
            if (item == null)
            {
                item = new TagInfo { Entry = entry, Example = word };
                items.Add(item);
            }
            item.TagsCount++;
        }

        private string Trim(string text)
        {
            return string.IsNullOrEmpty(text) ? text : text.Trim();
        }
    }
}
