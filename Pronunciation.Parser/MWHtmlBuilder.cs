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
                    var sounds = new List<SoundInfo>();
                    string entryNumber = addNumber ? item.Number.ToString() : null;

                    // If there's only one item with one audio then put audio button on the word level, not on the item level
                    if (!isFragment && entry.Items.Count == 1 && item.SoundFiles.Length == 1)
                    {
                        wordAudio = PrepareWordAudio(item.SoundFiles[0], sounds, entry.Keyword, entryNumber);
                    }
                    else
                    {
                        PrepareSounds(item.SoundFiles, sounds, bld, entry.Keyword, entryNumber);
                    }

                    if (wordDescription != null && sounds.Count > 0)
                    {
                        wordDescription.Sounds.AddRange(sounds);
                        if (!isFragment && string.IsNullOrEmpty(wordDescription.SoundKeyUS))
                        {
                            wordDescription.SoundKeyUS = sounds[0].SoundKey;
                        }
                    }
                }

                bld.Append(
@"
            </div>
");
            }

            if (entry.WordForms != null && entry.WordForms.Count > 0)
            {
                bld.Append(
@"          <div class=""forms"">
");
                foreach (var form in entry.WordForms)
                {
                    bld.AppendFormat(
@"              <div class=""form"">{0}</div>
",
                        PrepareWordForm(form, wordDescription));
                }

                bld.Append(
@"          </div>");
            }

            bld.Append(
@"      </div>
    </div>
");

            return bld.ToString();
        }

        private string PrepareWordForm(MWHtmlWordForm form, WordDescription wordDescription)
        {
            var bld = new StringBuilder();
            bld.AppendFormat(
@"<span class=""form_name"">{0}</span>", form.FormName);

            if (!string.IsNullOrEmpty(form.Note))
            {
                bld.AppendFormat(
@" <span class=""form_note"">{0}</span>", form.Note);
            }

            if (!string.IsNullOrEmpty(form.Transcription))
            {
                bld.AppendFormat(
@" <span class=""pron"">{0}</span>", PrepareTranscriptionHtml(form.Transcription));
            }

            if (form.SoundFiles != null && form.SoundFiles.Length > 0)
            {
                var sounds = new List<SoundInfo>();
                PrepareSounds(form.SoundFiles, sounds, bld, form.FormName, null);
                if (wordDescription != null && sounds.Count > 0)
                {
                    wordDescription.Sounds.AddRange(sounds);
                }
            }

            return bld.ToString();
        }

        private HtmlBuilder.WordAudio PrepareWordAudio(string soundFile, List<SoundInfo> sounds, 
            string targetWord, string entryNumber)
        {
            string soundKey = GetAudioKey(soundFile);
            sounds.Add(new SoundInfo(soundKey, false));

            return new HtmlBuilder.WordAudio 
            { 
                SoundTextUS =  _buttonBuilder.BuildHtml(AudioButtonStyle.BigUS, soundKey, targetWord, entryNumber, 0)
            };
        }

        private void PrepareSounds(string[] soundFiles, List<SoundInfo> sounds, StringBuilder bld, 
            string targetWord, string entryNumber)
        {
            int soundNumber = 0;
            foreach (var soundFile in soundFiles)
            {
                soundNumber++;
                string soundKey = GetAudioKey(soundFile);
                sounds.Add(new SoundInfo(soundKey, false));

                string buttonText = _buttonBuilder.BuildHtml(AudioButtonStyle.SmallUS, soundKey, targetWord, entryNumber,
                    soundFiles.Length == 1 ? 0 : soundNumber);
                bld.Append(buttonText);
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
