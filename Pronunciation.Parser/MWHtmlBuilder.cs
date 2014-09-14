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
        private readonly IFileLoader _fileLoader;
        private readonly bool _isDatabaseMode;

        public MWHtmlBuilder(HtmlBuilder.GenerationMode generationMode, MWHtmlEntry[] entries,
            IFileLoader fileLoader)
        {
            _isDatabaseMode = (generationMode == HtmlBuilder.GenerationMode.Database);
            _entries = entries.ToDictionary(x => x.Keyword);
            _fileLoader = fileLoader;
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
        <div class=""mw_header""></div>
        <div class=""mw_content"">
",
                isFragment ? "mw_fragment" : "mw_page");

            var textBuilder = new SoundTitleBuilder(entry.Keyword, entry.Items.Count);
            bool isMainAudioSet = false;
            bool addNumber = entry.Items.Count > 1;
            foreach (var item in entry.Items)
            {
                bld.Append(
@"      <div class=""mw_entry"">
");
                bld.AppendFormat(
@"          <span class=""mw_word_name"">{0}</span>",
                    item.DisplayName);

                if (addNumber)
                {
                    // Ensure there's no space before <span>
                    bld.AppendFormat(
@"<span class=""mw_entry_number""><sup>{0}</sup></span>",
                        item.Number);

                    if (!string.IsNullOrEmpty(item.PartsOfSpeech))
                    {
                        bld.AppendFormat(
@" <span class=""mw_speech_part"">{0}</span>", item.PartsOfSpeech);
                    }
                }

                if (!string.IsNullOrEmpty(item.Transcription))
                {
                    bld.AppendFormat(
@" <span class=""mw_pron"">{0}</span>", PrepareTranscriptionHtml(item.Transcription));
                }

                if (item.SoundFiles != null && item.SoundFiles.Length > 0)
                {
                    int soundNumber = 0;
                    foreach (var soundFile in item.SoundFiles)
                    {
                        if (item.SoundFiles.Length > 1)
                        {
                            soundNumber++;
                        }
                        string soundKey = GetAudioKey(soundFile);

                        // If there's only one item with one audio then put audio button on the word level, not on the item level
                        if (entry.Items.Count == 1 && !isFragment && item.SoundFiles.Length == 1)
                        {
                            wordAudio = new HtmlBuilder.WordAudio();
                            wordAudio.SoundTextUK = PrepareButtonText(HtmlBuilder.CaptionBigUS, soundNumber, soundKey);
                        }
                        else
                        {
                            bld.Append(PrepareButtonText(HtmlBuilder.CaptionSmallUS, soundNumber, soundKey));
                        }

                        if (wordDescription != null)
                        {
                            if (!isFragment && !isMainAudioSet)
                            {
                                wordDescription.SoundKeyUS = soundKey;
                                isMainAudioSet = true;
                            }

                            wordDescription.Sounds.Add(new SoundInfo(
                                soundKey,
                                textBuilder.GetSoundTitle(soundKey, item.Number.ToString()),
                                false));
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

        private string PrepareButtonText(string baseCaption, int soundNumber, string soundKey)
        {
            string caption = soundNumber > 0 ? string.Format("{0} {1}", baseCaption, soundNumber) : baseCaption;
            if (_isDatabaseMode)
            {
                return string.Format(
@" <button type=""button"" class=""audio_button audio_us"" data-src=""{0}"">{1}</button>",
                    soundKey, caption);
            }
            else
            {
                return string.Format(
@" <button type=""button"" class=""audio_button audio_us"" data-src=""{0}"" raw-data=""{1}"">{2}</button>",
                    soundKey, _fileLoader.GetBase64Content(soundKey), caption);
            }
        }

        private string PrepareTranscriptionHtml(string transcription)
        {
            if (string.IsNullOrEmpty(transcription))
                return transcription;

            return transcription
                .Replace(MWParser.TranscriptionRefOpenTag, "<span class=\"mw_reference\">")
                .Replace(MWParser.TranscriptionRefCloseTag, "</span>")
                .Replace(MWParser.TranscriptionItalicOpenTag, "<em>")
                .Replace(MWParser.TranscriptionItalicCloseTag, "</em>")
                .Replace(MWParser.TranscriptionUnderlinedOpenTag, "<span class=\"mw_underlined\">")
                .Replace(MWParser.TranscriptionUnderlinedCloseTag, "</span>")
                .Replace(MWParser.TranscriptionNoteOpenTag, "<span class=\"mw_pron_note\">")
                .Replace(MWParser.TranscriptionNoteCloseTag, "</span>")
                .Replace(MWParser.TranscriptionSeparatorOpenTag, "<span class=\"mw_pron_separator\">")
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
