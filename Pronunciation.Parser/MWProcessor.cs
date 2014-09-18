using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pronunciation.Parser
{
    class MWProcessor
    {
        public static void ParseDictionary(string dictionaryFile, string outputFile, string logFile)
        {
            var parser = new MWParser(logFile);
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
                        number,
                        item.ItemNumber,
                        PreparePartsOfSpeech(item.PartsOfSpeech),
                        item.Transcription,
                        PrepareSoundFiles(item.SoundFiles)));
                    bld.AppendLine();
                }
            }

            File.WriteAllText(outputFile, bld.ToString(), Encoding.UTF8);
        }

        public static MWHtmlEntry[] LoadParsedData(string sourceFile)
        {
            var entries = new Dictionary<string, MWHtmlEntry>();
            using (var reader = new StreamReader(sourceFile, Encoding.UTF8))
            {
                while (!reader.EndOfStream)
                {
                    string[] data = reader.ReadLine().Split('\t');
                    if (data.Length != 6)
                        throw new InvalidOperationException("Source MW file is broken!");

                    string keyword = data[0];
                    MWHtmlEntry entry;
                    if (!entries.TryGetValue(keyword, out entry))
                    {
                        entry = new MWHtmlEntry { Keyword = keyword, Items = new List<MWHtmlEntryItem>() };
                        entries.Add(keyword, entry);
                    }

                    entry.Items.Add(new MWHtmlEntryItem 
                    {
                        DisplayName = keyword,
                        Number = int.Parse(data[1]),
                        PartsOfSpeech = data[3],
                        Transcription = data[4],
                        SoundFiles = ParseSoundFiles(data[5])
                    });
                }
            }

            return entries.Values.OrderBy(x => x.Keyword).ToArray();
        }

        private static string PreparePartsOfSpeech(IEnumerable<string> partsOfSpeech)
        {
            return partsOfSpeech == null ? null : string.Join(", ", partsOfSpeech.Distinct().OrderBy(x => x));
        }

        private static string PrepareSoundFiles(IEnumerable<string> soundFiles)
        {
            // Do not sort sounds because they order often corresponds to the transcriptions order (see "dog")
            return soundFiles == null ? null : string.Join(", ", soundFiles);
        }

        private static string[] ParseSoundFiles(string soundFiles)
        {
            if (string.IsNullOrEmpty(soundFiles))
                return null;

            return soundFiles.Split(',')
                .Select(x => Trim(x))
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();
        }

        private static string Trim(string text)
        {
            return string.IsNullOrEmpty(text) ? text : text.Trim();
        }
    }
}
