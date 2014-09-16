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
        <div class=""dic_header""></div>
        <div class=""dic_content"">
",
                isFragment ? "mw_fragment" : "mw_page");

            var textBuilder = new SoundTitleBuilder(entry.Keyword, entry.Items.Count);
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
                        if (entry.Items.Count == 1 && !isFragment && isSingleAudio)
                        {
                            wordAudio = new HtmlBuilder.WordAudio();
                            wordAudio.SoundTextUK = PrepareButtonText(
                                HtmlBuilder.CaptionBigUS, isSingleAudio ? 0 : soundNumber, soundKey);
                        }
                        else
                        {
                            bld.Append(PrepareButtonText(
                                HtmlBuilder.CaptionSmallUS, isSingleAudio ? 0 : soundNumber, soundKey));
                        }

                        if (wordDescription != null)
                        {
                            if (!isFragment && !isMainAudioSet)
                            {
                                wordDescription.SoundKeyUS = soundKey;
                                isMainAudioSet = true;
                            }

                            string textEntryNumber = string.Format("{0}{1}", 
                                item.Number, isSingleAudio ? null : "." + soundNumber);                         
                            wordDescription.Sounds.Add(new SoundInfo(
                                soundKey,
                                textBuilder.GetSoundTitle(soundKey, textEntryNumber),
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
@"      </div>
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
