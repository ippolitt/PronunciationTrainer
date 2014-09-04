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
                    bld.AppendFormat(string.Join("\t",
                        entry.Keyword, 
                        number,
                        PreparePartsOfSpeech(item.PartsOfSpeech),
                        item.ItemStressedText,
                        GetTranscriptionUK(item.Transcription),
                        GetTranscriptionUS(item.Transcription),
                        item.SoundFileUK,
                        item.SoundFileUS));
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
                    if (data.Length != 8)
                        throw new InvalidOperationException("Source LDOCE file is broken!");

                    string keyword = data[0];
                    LDOCEHtmlEntry entry;
                    if (!entries.TryGetValue(keyword, out entry))
                    {
                        entry = new LDOCEHtmlEntry { Keyword = keyword, Items = new List<LDOCEHtmlEntity>() };
                        entries.Add(keyword, entry);
                    }

                    entry.Items.Add(new LDOCEHtmlEntity 
                    { 
                        Number = int.Parse(data[1]),
                        PartsOfSpeech = data[2],
                        DisplayName = data[3],
                        TranscriptionUK = data[4],
                        TranscriptionUS = data[5],
                        SoundFileUK = data[6],
                        SoundFileUS = data[7]
                    });
                }
            }

            return entries.Values.OrderBy(x => x.Keyword).ToArray();
        }

        private static string PreparePartsOfSpeech(IEnumerable<string> partsOfSpeech)
        {
            return partsOfSpeech == null ? null : string.Join(", ", partsOfSpeech.OrderBy(x => x));
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
