using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pronunciation.Parser
{
    class LDOCEProcessor
    {
        public static void ParseDictionary(string dictionaryFile, string outputFile, string logFile)
        {
            var parser = new LDOCEParser(logFile);
            var entries = parser.Parse(dictionaryFile);

            var bld = new StringBuilder();
            foreach (var entry in entries.OrderBy(x => x.Keyword))
            {
                int number = 0;
                foreach(var item in entry.Items)
                {
                    number++;
                    bld.Append(string.Join("\t",
                        entry.Keyword, 
                        entry.Language,
                        entry.IsDuplicate ? entry.DuplicateSourceKeyword : null,
                        number,
                        PreparePartsOfSpeech(item.PartsOfSpeech),
                        item.ItemTitle.Serialize(),
                        GetTranscriptionUK(item.Transcription),
                        GetTranscriptionUS(item.Transcription),
                        item.SoundFileUK,
                        item.SoundFileUS,
                        item.Notes));
                    bld.AppendLine();
                }
            }

            File.WriteAllText(outputFile, bld.ToString(), Encoding.UTF8);
        }

        public static LDOCEHtmlEntry[] LoadParsedData(string sourceFile)
        {
            var entries = new Dictionary<string, LDOCEHtmlEntry>();
            using (var reader = new StreamReader(sourceFile, Encoding.UTF8))
            {
                while (!reader.EndOfStream)
                {
                    string[] data = reader.ReadLine().Split('\t');
                    if (data.Length != 11)
                        throw new InvalidOperationException("Source LDOCE file is broken!");

                    string keyword = data[0];
                    LDOCEHtmlEntry entry;
                    if (!entries.TryGetValue(keyword, out entry))
                    {
                        entry = new LDOCEHtmlEntry { Keyword = keyword, Items = new List<LDOCEHtmlEntryItem>() };
                        if (!string.IsNullOrEmpty(data[1]))
                        {
                            entry.Language = (EnglishVariant)Enum.Parse(typeof(EnglishVariant), data[1]);
                        }
                        if (!string.IsNullOrEmpty(data[2]))
                        {
                            entry.SourceArticleKeyword = data[2];
                        }
                        entries.Add(keyword, entry);
                    }

                    entry.Items.Add(new LDOCEHtmlEntryItem 
                    { 
                        Number = int.Parse(data[3]),
                        PartsOfSpeech = data[4],
                        Title = DisplayName.Deserialize(data[5]),
                        TranscriptionUK = data[6],
                        TranscriptionUS = data[7],
                        SoundFileUK = data[8],
                        SoundFileUS = data[9],
                        Notes = data[10]
                    });
                }
            }

            return entries.Values.OrderBy(x => x.Keyword).ToArray();
        }

        private static string PreparePartsOfSpeech(IEnumerable<string> partsOfSpeech)
        {
            return partsOfSpeech == null ? null : string.Join(", ", partsOfSpeech.Distinct().OrderBy(x => x));
        }

        private static string GetTranscriptionUK(string transcription)
        {
            if (string.IsNullOrEmpty(transcription))
                return null;

            return Trim(transcription.Split('$')[0]);
        }

        private static string GetTranscriptionUS(string transcription)
        {
            if (string.IsNullOrEmpty(transcription))
                return null;

            var parts = transcription.Split('$');
            return parts.Length > 1 ? Trim(parts[1]) : null;
        }

        private static string Trim(string text)
        {
            return string.IsNullOrEmpty(text) ? text : text.Trim();
        }
    }
}
