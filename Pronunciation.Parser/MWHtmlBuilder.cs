using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pronunciation.Parser
{
    class MWHtmlBuilder
    {
        private readonly Dictionary<string, MWHtmlEntry> _entries;
        private readonly AudioButtonHtmlBuilder _buttonBuilder;
        private readonly HtmlBuilder.GenerationMode _generationMode;
        private readonly bool _embedSoundTitle;

        public MWHtmlBuilder(HtmlBuilder.GenerationMode generationMode, MWHtmlEntry[] entries,
            AudioButtonHtmlBuilder buttonBuilder)
        {
            _generationMode = generationMode;
            _entries = entries.ToDictionary(x => x.Keyword);
            _buttonBuilder = buttonBuilder;
        }

        public IEnumerable<MWHtmlEntry> GetEntries()
        {
            return _entries.Values;
        }

        public string GenerateFragmentHtml(MWHtmlEntry entry, WordDescription wordDescription)
        {
            HtmlBuilder.WordAudio wordAudio;
            return GenerateHtml(true, entry, wordDescription, out wordAudio);
        }

        public string GeneratePageHtml(MWHtmlEntry entry, WordDescription wordDescription, 
            out HtmlBuilder.WordAudio wordAudio)
        {
            return GenerateHtml(false, entry, wordDescription, out wordAudio);
        }

        public string GenerateHtml(bool isFragment, MWHtmlEntry entry, WordDescription wordDescription, 
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
                isFragment ? "mw_fragment" : "mw_page");

            bool isMainAudioSet = false;
            bool addNumber = entry.Items.Count > 1;
            foreach (var item in entry.Items)
            {
                bld.Append(
@"          <div class=""dic_entry"">
");
                bld.AppendFormat(
@"              <span class=""entry_name"">{0}</span>",
                    item.DisplayName);

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

                if (!string.IsNullOrEmpty(item.Transcription))
                {
                    bld.AppendFormat(
@" <span class=""pron"">{0}</span>", PrepareTranscriptionHtml(item.Transcription));
                }

                if (item.SoundFiles != null && item.SoundFiles.Length > 0)
                {
                    int soundNumber = 0;
                    bool isSingleAudio = item.SoundFiles.Length == 1;
                    foreach (var soundFile in item.SoundFiles)
                    {
                        soundNumber++;
                        string soundKey = GetAudioKey(soundFile);

                        // If there's only one item with one audio then put audio button on the word level, not on the item level
                        bool isWordAudio = entry.Items.Count == 1 && !isFragment && isSingleAudio;
                        string buttonText = _buttonBuilder.BuildHtml(
                            isWordAudio ? AudioButtonStyle.BigUS : AudioButtonStyle.SmallUS, 
                            soundKey,
                            entry.Keyword,
                            addNumber ? item.Number.ToString() : null,
                            isSingleAudio ? 0 : soundNumber);
                        if (isWordAudio)
                        {
                            wordAudio = new HtmlBuilder.WordAudio();
                            wordAudio.SoundTextUS = buttonText;
                        }
                        else
                        {
                            bld.Append(buttonText);
                        }

                        if (wordDescription != null)
                        {
                            if (!isFragment && !isMainAudioSet)
                            {
                                wordDescription.SoundKeyUS = soundKey;
                                isMainAudioSet = true;
                            }

                            wordDescription.Sounds.Add(new SoundInfo(soundKey, false));
                        }
                    }
                }

                bld.Append(
@"
            </div>
");
            }

            bld.Append(
@"      </div>
    </div>
");

            return bld.ToString();
        }

        private string PrepareTranscriptionHtml(string transcription)
        {
            if (string.IsNullOrEmpty(transcription))
                return transcription;

            return transcription
                .Replace(MWParser.TranscriptionRefOpenTag, "<span class=\"word_reference\">")
                .Replace(MWParser.TranscriptionRefCloseTag, "</span>")
                .Replace(MWParser.TranscriptionItalicOpenTag, "<em>")
                .Replace(MWParser.TranscriptionItalicCloseTag, "</em>")
                .Replace(MWParser.TranscriptionUnderlinedOpenTag, "<span class=\"underlined_text\">")
                .Replace(MWParser.TranscriptionUnderlinedCloseTag, "</span>")
                .Replace(MWParser.TranscriptionNoteOpenTag, "<span class=\"pron_note\">")
                .Replace(MWParser.TranscriptionNoteCloseTag, "</span>")
                .Replace(MWParser.TranscriptionSeparatorOpenTag, "<span class=\"pron_separator\">")
                .Replace(MWParser.TranscriptionSeparatorCloseTag, "</span>");
        }

        private string GetAudioKey(string audioFile)
        {
            if (string.IsNullOrEmpty(audioFile))
                return null;

            return SoundManager.MW_SoundKeyPrefix + Path.GetFileNameWithoutExtension(audioFile);
        }
    }
}
