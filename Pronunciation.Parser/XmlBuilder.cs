using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace Pronunciation.Parser
{
    class XmlBuilder
    {
        public const string ElementRoot = "dic";
        public const string ElementKeyword = "word";
        public const string ElementKeywordName = "name";
        public const string ElementEntryRoot = "entry";
        public const string ElementEntryNumber = "number";
        public const string ElementEntryData = "data";
        public const string ElementComment = "note";
        public const string ElementWordForm = "form";
        public const string ElementCollocation = "collocation";
        public const string ElementCombImage = "pic_comb";
        public const string ElementImage = "pic";

        private readonly string _logFile;

        public XmlBuilder(string logFile)
        {
            _logFile = logFile;
        }

        public void ConvertToXml(string source, string target, bool replaceAllTags, bool analyzeData)
        {
            using (var reader = new StreamReader(source))
            {
                var settings = new XmlWriterSettings() 
                { 
                    Encoding = Encoding.Unicode,
                    Indent = true
                };

                using (var dest = XmlWriter.Create(target, settings))
                {
                    dest.WriteProcessingInstruction("xml-stylesheet", "type='text/css' href='Sample.css'");
                    dest.WriteStartElement(ElementRoot);

                    File.AppendAllText(_logFile, "********* Starting conversion ***********\r\n");

                    var analyzer = new DicAnalyzer();
                    var tracker = new StructureTracker();
                    var tags = new TagAnalyzer();
                    bool isPendingWord = false;
                    bool isPendingEntry = false;
                    int lineCount = 0;
                    int keywordsCount = 0;
                    while (!reader.EndOfStream)
                    {
                        var text = reader.ReadLine().Trim();
                        if (string.IsNullOrEmpty(text))
                            continue;

                        var element = analyzer.ParseLine(text);
                        if (element is KeywordElement)
                        {
                            //if (keywordsCount > 1000)
                            //    break;

                            var keyword = ((KeywordElement)element).Keyword;
                            if (analyzeData)
                            {
                                tags.AnalyzeText(EntryType.Keyword, keyword, keyword);
                            }

                            if (replaceAllTags)
                            {
                                keyword = tags.ConvertText(EntryType.Keyword, keyword, keyword);
                            }
                            tracker.RegisterKeyword(keyword);
                            keywordsCount++;

                            if (isPendingEntry)
                            {
                                dest.WriteEndElement();
                                isPendingEntry = false;
                            }
                            
                            if (isPendingWord)
                            {
                                dest.WriteEndElement();
                            }
                            dest.WriteStartElement(ElementKeyword);
                            isPendingWord = true;

                            AddNode(dest, ElementKeywordName, keyword);
                        }
                        else if (element is EntryNumberElement)
                        {
                            tracker.RegisterEntryNumber(((EntryNumberElement)element).Number);
                        }
                        else if (element is EntryElement)
                        {
                            var convertedText = element.Text;
                            if (analyzeData)
                            {
                                tags.AnalyzeText(((EntryElement)element).EntryType, convertedText, tracker.Keyword);
                            }

                            if (replaceAllTags)
                            {
                                convertedText = tags.ConvertText(((EntryElement)element).EntryType, convertedText, tracker.Keyword);
                            }

                            switch (((EntryElement)element).EntryType)
                            {
                                case EntryType.MainEntry:
                                    tracker.RegisterEntry();

                                    if (isPendingEntry)
                                    {
                                        dest.WriteEndElement();
                                    }
                                    dest.WriteStartElement(ElementEntryRoot);
                                    isPendingEntry = true;

                                    if (!string.IsNullOrEmpty(tracker.EntryNumber))
                                    {
                                        AddNode(dest, ElementEntryNumber, tracker.EntryNumber);
                                    }
                                    AddNode(dest, ElementEntryData, convertedText);
                                    break;

                                case EntryType.Comment:
                                    tracker.RegisterComment();
                                    AddNode(dest, ElementComment, convertedText);
                                    break;

                                case EntryType.Image:
                                    tracker.RegisterImage();
                                    AddNode(dest, ElementImage, convertedText);
                                    break;

                                case EntryType.ImageComb:
                                    tracker.RegisterImage();
                                    AddNode(dest, ElementCombImage, convertedText);
                                    break;

                                case EntryType.WordForm:
                                    tracker.RegisterEntryForm();
                                    AddNode(dest, ElementWordForm, convertedText);
                                    break;

                                case EntryType.Collocation:
                                    tracker.RegisterEntryCollocation();
                                    AddNode(dest, ElementCollocation, convertedText);
                                    break;

                                default:
                                    throw new NotSupportedException();
                            }
                        }
                        else
                        {
                            File.AppendAllText(_logFile, string.Format("-[ignored text] {0}\r\n", text));
                        }

                        lineCount++;
                    }

                    if (isPendingEntry)
                    {
                        dest.WriteEndElement();
                    }
                    if (isPendingWord)
                    {
                        dest.WriteEndElement();
                    }
                    dest.WriteEndElement();

                    if (tags.BldStats.Length > 0)
                    {
                        File.AppendAllText(_logFile, "** Ame analysis ** \r\n" + tags.BldStats.ToString());
                    }

                    if (analyzeData)
                    {
                        WriteStats(tags);
                    }
                }
            }
        }

        private void WriteStats(TagAnalyzer tags)
        {
            StringBuilder bld = new StringBuilder("********* Tag analysis ***********\r\n");
            foreach (var entry in tags.Tags.OrderBy(x => x.Key))
            {
                bld.AppendFormat("-[{0}]-", entry.Key);
                foreach (var val in entry.Value.OrderBy(x => (int)x.Entry))
                {
                    bld.AppendFormat("  {0}: {1} ({2});", val.Entry, val.TagsCount, val.Example);
                }
                bld.AppendLine();
            }

            File.AppendAllText(_logFile, bld.ToString());
        }

        private static void AddNode(XmlWriter writer, string nodeName, string text)
        {
            writer.WriteStartElement(nodeName);
            writer.WriteRaw(text);
            writer.WriteEndElement();
        }
    }
}
