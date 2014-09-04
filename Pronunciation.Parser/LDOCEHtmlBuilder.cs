using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pronunciation.Parser
{
    class LDOCEHtmlBuilder
    {
        private readonly Dictionary<string, LDOCEHtmlEntry> _entries;
        private readonly IFileLoader _fileLoader;
        public const string AudioKeyPrefics = "ldoce_";

        public LDOCEHtmlBuilder(LDOCEHtmlEntry[] entries, IFileLoader fileLoader)
        {
            _entries = entries.ToDictionary(x => x.Keyword);
            _fileLoader = fileLoader;
        }

        public LDOCEHtmlEntry GetEntry(string keyword)
        {
            LDOCEHtmlEntry entry;
            _entries.TryGetValue(keyword, out entry);

            return entry;
        }

        public IEnumerable<LDOCEHtmlEntry> GetExtraEntries(IEnumerable<string> existingKeywords)
        {
            var keywords = new HashSet<string>(existingKeywords);
            return _entries.Where(x => !keywords.Contains(x.Key)).Select(x => x.Value);
        }

        public string GenerateFragmentHtml(LDOCEHtmlEntry entry)
        {
            HtmlBuilder.WordAudio wordAudio;
            return GenerateHtml(true, entry, false, null, out wordAudio);
        }

        public string GeneratePageHtml(LDOCEHtmlEntry entry, bool useAudioKey, WordDescription wordDescription, 
            out HtmlBuilder.WordAudio wordAudio)
        {
            return GenerateHtml(false, entry, useAudioKey, wordDescription, out wordAudio);
        }

        public string GenerateHtml(bool isFragment, LDOCEHtmlEntry entry, bool useAudioKey, WordDescription wordDescription, 
            out HtmlBuilder.WordAudio wordAudio)
        {
            wordAudio = null;

            var bld = new StringBuilder();
            bld.AppendFormat(
@"
    <div class=""{0}"">
        <div class=""ldoce_header""></div>
        <div class=""ldoce_content"">
",
                isFragment ? "ldoce_fragment" : "ldoce_page");

            bool isMainAudioSet = false;
            bool addNumber = entry.Items.Count > 1;
            foreach (var item in entry.Items)
            {
                bld.Append(
@"      <div class=""ldoce_entry"">
");
                bld.AppendFormat(
@"          <span class=""ldoce_word_name"">{0}</span>",
                    PrepareDisplayNameHtml(item.DisplayName));

                if (addNumber)
                {
                    // Ensure there's no space before <span>
                    bld.AppendFormat(
@"<span class=""ldoce_entry_number""><sup>{0}</sup></span>",
                        item.Number);
                }

                if (!string.IsNullOrEmpty(item.PartsOfSpeech))
                {
                    bld.AppendFormat(
@" <span class=""ldoce_speech_part"">{0}</span>", item.PartsOfSpeech);
                }

                if (!string.IsNullOrEmpty(item.TranscriptionUK))
                {
                    bld.AppendFormat(
@" <span class=""ldoce_pron"">{0}</span>", PrepareTranscriptionHtml(item.TranscriptionUK));
                }

                if (!string.IsNullOrEmpty(item.TranscriptionUS))
                {
                    bld.AppendFormat(
@" <span class=""ldoce_pron_us"">{0}</span>", PrepareTranscriptionHtml(item.TranscriptionUS));
                }

                if (!isFragment)
                {
                    bool hasUKAudio = !string.IsNullOrEmpty(item.SoundFileUK);
                    bool hasUSAudio = !string.IsNullOrEmpty(item.SoundFileUS);
                    if (entry.Items.Count == 1)
                    {
                        // If there's only one item then put audio buttons on the word level, not on the item level
                        wordAudio = new HtmlBuilder.WordAudio();
                        if (hasUKAudio)
                        {
                            wordAudio.SoundTextUK = PrepareButtonText(useAudioKey, HtmlBuilder.CaptionBigUK, item.SoundFileUK, true);
                        }
                        if (hasUSAudio)
                        {
                            wordAudio.SoundTextUS = PrepareButtonText(useAudioKey, HtmlBuilder.CaptionBigUS, item.SoundFileUS, false);
                        }
                    }
                    else
                    {
                        if (hasUKAudio)
                        {
                            bld.Append(PrepareButtonText(useAudioKey, HtmlBuilder.CaptionSmallUK, item.SoundFileUK, true));
                        }
                        if (hasUSAudio)
                        {
                            bld.Append(PrepareButtonText(useAudioKey, HtmlBuilder.CaptionSmallUS, item.SoundFileUS, false));
                        }
                    }

                    if (wordDescription != null)
                    {
                        if (!isMainAudioSet && (hasUKAudio || hasUSAudio))
                        {
                            wordDescription.SoundKeyUK = GetAudioKey(item.SoundFileUK);
                            wordDescription.SoundKeyUS = GetAudioKey(item.SoundFileUS);
                            isMainAudioSet = true;
                        }

                        if (hasUKAudio)
                        {
                            wordDescription.Sounds.Add(new SoundInfo(GetAudioKey(item.SoundFileUK), true));
                        }
                        if (hasUSAudio)
                        {
                            wordDescription.Sounds.Add(new SoundInfo(GetAudioKey(item.SoundFileUS), false));
                        }
                    }
                }

                bld.Append(
@"
        </div>
");
            }

            bld.Append(
@"
        </div>
    </div>
");

            return bld.ToString();
        }

        private string PrepareButtonText(bool isAudioKey, string caption, string audioFile, bool isUkAudio)
        {
            string styleName = isUkAudio ? "audio_uk" : "audio_us";
            if (isAudioKey)
            {
                return string.Format(
@" <button type=""button"" class=""audio_button {0}"" data-src=""{1}"">{2}</button>",
                    styleName, GetAudioKey(audioFile), caption);
            }
            else
            {
                return string.Format(
@" <button type=""button"" class=""audio_button {0}"" data-src=""{1}"" raw-data=""{2}"">{3}</button>",
                    styleName, GetAudioKey(audioFile), GetAudioContent(audioFile), caption);
            }
        }

        private string PrepareDisplayNameHtml(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
                return displayName;

            return displayName
                .Replace("ˈ", "<span class=\"ldoce_stress_up\"></span>")
                .Replace("ˌ", "<span class=\"ldoce_stress_low\"></span>");
        }

        private string PrepareTranscriptionHtml(string transcription)
        {
            if (string.IsNullOrEmpty(transcription))
                return transcription;

            return transcription
                .Replace(LDOCEParser.TranscriptionNoteOpenTag, "<span class=\"ldoce_pron_note\">")
                .Replace(LDOCEParser.TranscriptionNoteCloseTag, "</span>")
                .Replace(LDOCEParser.TranscriptionItalicOpenTag, "<em>")
                .Replace(LDOCEParser.TranscriptionItalicCloseTag, "</em>")
                .Replace("◂", "<span class=\"ldoce_stress_shift\"></span>");
        }

        private string GetAudioContent(string audioFile)
        {
            if (string.IsNullOrEmpty(audioFile))
                return null;

            return _fileLoader.GetBase64Content(GetAudioKey((audioFile)));
        }

        private string GetAudioKey(string audioFile)
        {
            if (string.IsNullOrEmpty(audioFile))
                return null;

            return AudioKeyPrefics + Path.GetFileNameWithoutExtension(audioFile);
        }
    }
}
