﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    class XmlReplaceMap
    {
        private readonly List<TagReplaceInfo> _map = new List<TagReplaceInfo>();

        public const string XmlElementStrong = "strong";

        public XmlReplaceMap(bool isDatabaseMode)
        {
            _map = new List<TagReplaceInfo>();

            AddMap(XmlElementStrong, ReplacementType.LeaveOldTag, null, null);
            AddMap("em", ReplacementType.LeaveOldTag, null, null);
            AddMap("sub", ReplacementType.LeaveOldTag, null, null);
            AddMap("sup", ReplacementType.LeaveOldTag, null, null);

            AddMap(XmlBuilder.ElementComment, ReplacementType.ReplaceOldTag, "div", "class=\"comment\"");
            AddMap("pron", ReplacementType.ReplaceOldTag, "span", "class=\"pron\"");
            AddMap("pron_us", ReplacementType.ReplaceOldTag, "span", "class=\"pron_us\"");
            AddMap("pron_us_alt", ReplacementType.ReplaceOldTag, "span", "class=\"pron_us_alt\"");
            AddMap("pron_other", ReplacementType.ReplaceOldTag, "span", "class=\"pron_other\"");
            AddMap("sample", ReplacementType.ReplaceOldTag, "span", "class=\"sample\"");
            AddMap("lang", ReplacementType.ReplaceOldTag, "span", "class=\"lang\"");
            AddMap("stress_up", ReplacementType.ReplaceOldTag, "span", "class=\"stress_up\"", false);
            AddMap("stress_low", ReplacementType.ReplaceOldTag, "span", "class=\"stress_low\"", false);
            AddMap("stress_low_optional", ReplacementType.ReplaceOldTag, "span", "class=\"stress_low_optional\"", true);
            AddMap("stress_shift", ReplacementType.ReplaceOldTag, "span", "class=\"stress_shift\"", false);

            AddMap("pic", ReplacementType.ReplaceImage, "img",
                "<img class=\"poll_image\" src=\"{0}\" />", false);
            AddMap("wlink", ReplacementType.ReplaceLink, "a",
                "<a class=\"word_link\" href=\"{0}\">{1}</a>", false);
            if (isDatabaseMode)
            {
                AddMap("sound_uk", ReplacementType.ReplaceSoundUK, "button",
                    "<button type=\"button\" class=\"audio_button audio_uk\" data-src=\"{0}\">{1}</button>",
                    false);
                AddMap("sound_us", ReplacementType.ReplaceSoundUS, "button",
                    "<button type=\"button\" class=\"audio_button audio_us\" data-src=\"{0}\">{1}</button>",
                    false);
            }
            else
            {
                AddMap("sound_uk", ReplacementType.ReplaceSoundUK, "button",
                    "<button type=\"button\" class=\"audio_button audio_uk\" data-src=\"{0}\" raw-data=\"{1}\">{2}</button>",
                    false);
                AddMap("sound_us", ReplacementType.ReplaceSoundUS, "button",
                    "<button type=\"button\" class=\"audio_button audio_us\" data-src=\"{0}\" raw-data=\"{1}\">{2}</button>",
                    false);
            }
        }

        public TagReplaceInfo GetReplaceInfo(string tagName)
        {
            return _map.Single(x => x.SourceTag == tagName);
        }

        private void AddMap(string sourceTag, ReplacementType replacementType, string replacementTag, string additionalData)
        {
            AddMap(sourceTag, replacementType, replacementTag, additionalData, true);
        }

        private void AddMap(string sourceTag, ReplacementType replacementType, string replacementTag,
            string additionalData, bool allowChildTags)
        {
            _map.Add(new TagReplaceInfo
            {
                SourceTag = sourceTag,
                ReplacementType = replacementType,
                ReplacementTag = replacementTag,
                AdditionalData = additionalData,
                AllowChildTags = allowChildTags
            });
        }
    }

    class TagReplaceInfo
    {
        public string SourceTag;
        public ReplacementType ReplacementType;
        public string ReplacementTag;
        public string AdditionalData;
        public bool AllowChildTags;
    }

    class CurrentTagInfo
    {
        public TagReplaceInfo ReplaceInfo;
        public string Data;
    }

    enum ReplacementType
    {
        LeaveOldTag,
        ReplaceOldTag,
        ReplaceImage,
        ReplaceLink,
        ReplaceSoundUK,
        ReplaceSoundUS,
    }
}
