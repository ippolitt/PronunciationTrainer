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
        private readonly bool _isDatabaseMode;

        public LDOCEHtmlBuilder(HtmlBuilder.GenerationMode generationMode, LDOCEHtmlEntry[] entries,
            IFileLoader fileLoader)
        {
            _isDatabaseMode = (generationMode == HtmlBuilder.GenerationMode.Database);
            _entries = entries.ToDictionary(x => x.Keyword);
            _fileLoader = fileLoader;
        }

        public IEnumerable<LDOCEHtmlEntry> GetEntries()
        {
            return _entries.Values;
        }

        public string GenerateFragmentHtml(LDOCEHtmlEntry entry)
        {
            HtmlBuilder.WordAudio wordAudio;
            return GenerateHtml(true, entry, null, out wordAudio);
        }

        public string GeneratePageHtml(LDOCEHtmlEntry entry, WordDescription wordDescription, 
            out HtmlBuilder.WordAudio wordAudio)
        {
            return GenerateHtml(false, entry, wordDescription, out wordAudio);
        }

        public string GenerateHtml(bool isFragment, LDOCEHtmlEntry entry, WordDescription wordDescription, 
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

            var textBuilder = new SoundTitleBuilder(entry.Keyword, entry.Items.Count);
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

                    if (!string.IsNullOrEmpty(item.PartsOfSpeech))
                    {
                        bld.AppendFormat(
@" <span class=""ldoce_speech_part"">{0}</span>", item.PartsOfSpeech);
                    }
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
                    string soundKeyUK = GetAudioKey(item.SoundFileUK);
                    string soundKeyUS = GetAudioKey(item.SoundFileUS);
                    if (entry.Items.Count == 1)
                    {
                        // If there's only one item then put audio buttons on the word level, not on the item level
                        wordAudio = new HtmlBuilder.WordAudio();
                        if (hasUKAudio)
                        {
                            wordAudio.SoundTextUK = PrepareButtonText(HtmlBuilder.CaptionBigUK, soundKeyUK, true);
                        }
                        if (hasUSAudio)
                        {
                            wordAudio.SoundTextUS = PrepareButtonText(HtmlBuilder.CaptionBigUS, soundKeyUS, false);
                        }
                    }
                    else
                    {
                        if (hasUKAudio)
                        {
                            bld.Append(PrepareButtonText(HtmlBuilder.CaptionSmallUK, soundKeyUK, true));
                        }
                        if (hasUSAudio)
                        {
                            bld.Append(PrepareButtonText(HtmlBuilder.CaptionSmallUS, soundKeyUS, false));
                        }
                    }

                    if (wordDescription != null)
                    {
                        if (!isMainAudioSet && (hasUKAudio || hasUSAudio))
                        {
                            wordDescription.SoundKeyUK = soundKeyUK;
                            wordDescription.SoundKeyUS = soundKeyUS;
                            isMainAudioSet = true;
                        }

                        if (hasUKAudio)
                        {
                            wordDescription.Sounds.Add(new SoundInfo(soundKeyUK, 
                                textBuilder.GetSoundTitle(soundKeyUK, item.Number.ToString()), true));
                        }
                        if (hasUSAudio)
                        {
                            wordDescription.Sounds.Add(new SoundInfo(soundKeyUS, 
                                textBuilder.GetSoundTitle(soundKeyUS, item.Number.ToString()), false));
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

        private string PrepareButtonText(string caption, string soundKey, bool isUkAudio)
        {
            string styleName = isUkAudio ? "audio_uk" : "audio_us";
            if (_isDatabaseMode)
            {
                return string.Format(
@" <button type=""button"" class=""audio_button {0}"" data-src=""{1}"">{2}</button>",
                    styleName, soundKey, caption);
            }
            else
            {
                return string.Format(
@" <button type=""button"" class=""audio_button {0}"" data-src=""{1}"" raw-data=""{2}"">{3}</button>",
                    styleName, soundKey, _fileLoader.GetBase64Content(soundKey), caption);
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
                .Replace(LDOCEParser.TranscriptionSeparatorOpenTag, "<span class=\"ldoce_pron_separator\">")
                .Replace(LDOCEParser.TranscriptionSeparatorCloseTag, "</span>")
                .Replace("◂", "<span class=\"ldoce_stress_shift\"></span>");
        }

        private string GetAudioKey(string audioFile)
        {
            if (string.IsNullOrEmpty(audioFile))
                return null;

            return SoundManager.LDOCE_SoundKeyPrefix + Path.GetFileNameWithoutExtension(audioFile);
        }
    }
}
