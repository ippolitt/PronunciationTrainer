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
                        item.ItemTitle.Serialize(),
                        number,
                        item.ItemNumber,
                        PreparePartsOfSpeech(item.PartsOfSpeech),
                        item.Transcription,
                        PrepareSoundFiles(item.SoundFiles),
                        PrepareWordForms(item.WordForms)));
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
                    if (data.Length != 8)
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
                        Title = DisplayName.Deserialize(data[1]),
                        Number = int.Parse(data[2]),
                        PartsOfSpeech = data[4],
                        Transcription = data[5],
                        SoundFiles = ParseSoundFiles(data[6])
                    });

                    var wordForms = ParseWordForms(data[7]);
                    if (wordForms != null && wordForms.Length > 0)
                    {
                        if (entry.WordForms == null)
                        {
                            entry.WordForms = new List<MWHtmlWordForm>();
                        }
                        entry.WordForms.AddRange(wordForms);
                    }
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

        private static string PrepareWordForms(IEnumerable<MWWordForm> wordForms)
        {
            if (wordForms == null)
                return null;

            return string.Join("||", wordForms.Select(x => string.Format("{0}%%{1}%%{2}%%{3}",
                x.Title.Serialize(), x.IsPluralForm ? "plural" : null, x.Transcription, PrepareSoundFiles(x.SoundFiles))));
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

        private static MWHtmlWordForm[] ParseWordForms(string wordForms)
        {
            if (string.IsNullOrEmpty(wordForms))
                return null;

            var forms = wordForms.Split(new[] {"||"}, StringSplitOptions.None);
            var results = new MWHtmlWordForm[forms.Length];
            for (int i = 0; i < forms.Length; i++)
            {
                var parts = forms[i].Split(new[] { "%%" }, StringSplitOptions.None);
                if (parts.Length != 4)
                    throw new ArgumentException();

                results[i] = new MWHtmlWordForm 
                { 
                    Title = DisplayName.Deserialize(parts[0]),
                    Note = parts[1],
                    Transcription = parts[2],
                    SoundFiles = ParseSoundFiles(parts[3])
                };
            }

            return results;
        }

        private static string Trim(string text)
        {
            return string.IsNullOrEmpty(text) ? text : text.Trim();
        }
    }
}
