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
        private readonly AudioButtonHtmlBuilder _buttonBuilder;
        private readonly HtmlBuilder.GenerationMode _generationMode;

        public LDOCEHtmlBuilder(HtmlBuilder.GenerationMode generationMode, LDOCEHtmlEntry[] entries,
            AudioButtonHtmlBuilder buttonBuilder)
        {
            _generationMode = generationMode;
            _entries = entries.ToDictionary(x => x.Keyword);
            _buttonBuilder = buttonBuilder;
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
        <div class=""dic_header""></div>
        <div class=""dic_content"">
",
                isFragment ? "ldoce_fragment" : "ldoce_page");

            bool isMainAudioSet = false;
            bool addNumber = entry.Items.Count > 1;
            foreach (var item in entry.Items)
            {
                bld.Append(
@"      <div class=""dic_entry"">
");
                bld.AppendFormat(
@"          <span class=""entry_name"">{0}</span>",
                    PrepareDisplayNameHtml(item.DisplayName));

                if (addNumber)
                {
                    // Ensure there's no space before <span>
                    bld.AppendFormat(
@"<span class=""entry_number""><sup>{0}</sup></span>",
                        item.Number);

                    if (!string.IsNullOrEmpty(item.PartsOfSpeech))
                    {
                        bld.AppendFormat(
@" <span class=""speech_part"">{0}</span>", item.PartsOfSpeech);
                    }
                }

                if (!string.IsNullOrEmpty(item.TranscriptionUK))
                {
                    bld.AppendFormat(
@" <span class=""pron"">{0}</span>", PrepareTranscriptionHtml(item.TranscriptionUK));
                }

                if (!string.IsNullOrEmpty(item.TranscriptionUS))
                {
                    bld.AppendFormat(
@" <span class=""pron_us"">{0}</span>", PrepareTranscriptionHtml(item.TranscriptionUS));
                }

                if (!isFragment)
                {
                    bool hasUKAudio = !string.IsNullOrEmpty(item.SoundFileUK);
                    bool hasUSAudio = !string.IsNullOrEmpty(item.SoundFileUS);
                    string soundKeyUK = GetAudioKey(item.SoundFileUK);
                    string soundKeyUS = GetAudioKey(item.SoundFileUS);

                    string htmlEntryNumber = addNumber ? item.Number.ToString() : null;
                    if (entry.Items.Count == 1)
                    {
                        // If there's only one item then put audio buttons on the word level, not on the item level
                        wordAudio = new HtmlBuilder.WordAudio();
                        if (hasUKAudio)
                        {
                            wordAudio.SoundTextUK = _buttonBuilder.BuildHtml(
                                AudioButtonStyle.BigUK, soundKeyUK, entry.Keyword, htmlEntryNumber);
                        }
                        if (hasUSAudio)
                        {
                            wordAudio.SoundTextUS = _buttonBuilder.BuildHtml(
                                AudioButtonStyle.BigUS, soundKeyUS, entry.Keyword, htmlEntryNumber);
                        }
                    }
                    else
                    {
                        if (hasUKAudio)
                        {
                            bld.Append(_buttonBuilder.BuildHtml(
                                AudioButtonStyle.SmallUK, soundKeyUK, entry.Keyword, htmlEntryNumber));
                        }
                        if (hasUSAudio)
                        {
                            bld.Append(_buttonBuilder.BuildHtml(
                                AudioButtonStyle.SmallUS, soundKeyUS, entry.Keyword, htmlEntryNumber));
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
                            wordDescription.Sounds.Add(new SoundInfo(soundKeyUK, true));
                        }
                        if (hasUSAudio)
                        {
                            wordDescription.Sounds.Add(new SoundInfo(soundKeyUS, false));
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

        private string PrepareDisplayNameHtml(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
                return displayName;

            return displayName
                .Replace("ˈ", "<span class=\"stress_up\"></span>")
                .Replace("ˌ", "<span class=\"stress_low\"></span>");
        }

        private string PrepareTranscriptionHtml(string transcription)
        {
            if (string.IsNullOrEmpty(transcription))
                return transcription;

            return transcription
                .Replace(LDOCEParser.TranscriptionNoteOpenTag, "<span class=\"pron_note\">")
                .Replace(LDOCEParser.TranscriptionNoteCloseTag, "</span>")
                .Replace(LDOCEParser.TranscriptionItalicOpenTag, "<em>")
                .Replace(LDOCEParser.TranscriptionItalicCloseTag, "</em>")
                .Replace(LDOCEParser.TranscriptionSeparatorOpenTag, "<span class=\"pron_separator\">")
                .Replace(LDOCEParser.TranscriptionSeparatorCloseTag, "</span>")
                .Replace("◂", "<span class=\"stress_shift\"></span>");
        }

        private string GetAudioKey(string audioFile)
        {
            if (string.IsNullOrEmpty(audioFile))
                return null;

            return SoundManager.LDOCE_SoundKeyPrefix + Path.GetFileNameWithoutExtension(audioFile);
        }
    }
}
