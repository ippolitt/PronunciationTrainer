using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    public enum AudioButtonStyle
    {
        BigUK,
        BigUS,
        SmallUK,
        SmallUS
    }

    class AudioButtonHtmlBuilder
    {
        private readonly HtmlBuilder.GenerationMode _generationMode;
        private readonly static Dictionary<AudioButtonStyle, string> _captionMap;

        public bool IsContentLoadDisabled { get; set; } 

        static AudioButtonHtmlBuilder()
        {
            _captionMap = new Dictionary<AudioButtonStyle, string> 
            { 
                { AudioButtonStyle.BigUK, "British"},
                { AudioButtonStyle.BigUS, "American"},
                { AudioButtonStyle.SmallUK, "BrE"},
                { AudioButtonStyle.SmallUS, "AmE"}
            };
        }

        public AudioButtonHtmlBuilder(HtmlBuilder.GenerationMode generationMode)
        {
            _generationMode = generationMode;
        }

        public string BuildHtml(AudioButtonStyle buttonStyle, string soundKey)
        {
            return BuildHtml(buttonStyle, soundKey, null, null, 0);
        }

        public string BuildHtml(AudioButtonStyle buttonStyle, string soundKey, string targetWord, string entryNumber)
        {
            return BuildHtml(buttonStyle, soundKey, targetWord, entryNumber, 0);
        }

        public string BuildHtml(AudioButtonStyle buttonStyle, string soundKey, string targetWord, string entryNumber, int soundNumber)
        {
            string styleName = (buttonStyle == AudioButtonStyle.BigUK || buttonStyle == AudioButtonStyle.SmallUK) 
                ? "audio_uk" : "audio_us";
            string buttonCaption = string.Format("{0}{1}", _captionMap[buttonStyle], soundNumber > 0 ? " " + soundNumber : null);
            string titleAttribute = _generationMode == HtmlBuilder.GenerationMode.IPhone 
                ? null 
                : BuildSoundTitleAttribute(targetWord, entryNumber, soundNumber);

            return string.Format(
@" <button type=""button"" class=""audio_button {0}"" {2}data-src=""{1}"">{3}</button>",
                styleName, soundKey, titleAttribute, buttonCaption);
        }

        public void InjectSoundText(HtmlBuilder.ParseResult parseResult, string targetWord, string entryNumber, 
            List<SoundInfo> sounds)
        {
            if (_generationMode == HtmlBuilder.GenerationMode.IPhone || sounds == null || sounds.Count <= 0)
                return;

            // We need to run replacement separately for UK & US because we rely on the number of sounds
            parseResult.HtmlData = InjectSoundText(parseResult.HtmlData, targetWord, entryNumber,
                sounds.Where(x => x.IsUKSound));
            parseResult.HtmlData = InjectSoundText(parseResult.HtmlData, targetWord, entryNumber,
                sounds.Where(x => !x.IsUKSound));

            parseResult.SoundTextUK = InjectSoundText(parseResult.SoundTextUK, targetWord, entryNumber, 
                sounds.Where(x => x.IsUKSound));
            parseResult.SoundTextUS = InjectSoundText(parseResult.SoundTextUS, targetWord, entryNumber,
                sounds.Where(x => !x.IsUKSound));
        }

        private string InjectSoundText(string html, string targetWord, string entryNumber, 
            IEnumerable<SoundInfo> sounds)
        {
            if (string.IsNullOrEmpty(html))
                return html;

            StringBuilder bld = new StringBuilder(html);
            int soundNumber = 0;
            var arrSounds = sounds.ToArray();
            bool addNumber = arrSounds.Length > 1;
            foreach (var sound in arrSounds)
            {
                soundNumber++;

                string replaceText = string.Format("data-src=\"{0}\"", sound.SoundKey);
                string soundTitleAttribute = addNumber 
                    ? BuildSoundTitleAttribute(targetWord, entryNumber, soundNumber)
                    : BuildSoundTitleAttribute(targetWord, entryNumber, 0);
                if (!string.IsNullOrEmpty(soundTitleAttribute))
                {
                    bld.Replace(replaceText, string.Format("{0}{1}", soundTitleAttribute, replaceText));
                }
            }

            return bld.ToString();
        }

        private string BuildSoundTitleAttribute(string targetWord, string entryNumber, int soundNumber)
        {
            if (string.IsNullOrEmpty(targetWord))
                return null;

            //string title;
            //if (string.IsNullOrEmpty(entryNumber))
            //{
            //    title = string.Format("{0}{1}", targetWord, 
            //        soundNumber > 0 ? ", " + soundNumber : null);
            //}
            //else
            //{
            //    title = string.Format("{0}, {1}{2}", targetWord, entryNumber, 
            //        soundNumber > 0 ? "." + soundNumber : null);
            //}

            return string.Format("audio_title=\"{0}\" ", HtmlHelper.PrepareAttributeValue(targetWord));
        }
    }
}
