using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation
{
    public class DicAnalyzer
    {
        private const string EntryOpenTag = "[m1]";
        private const string WordFormOpenTag = "[m1]▷";
        private const string CollocationOpenTag = "[m1]▶";
        private const string CommentOpenTag = "[m2]";
        private const string CloseTag = "[/m]";

        private const string ImageTag = "[s]";
        private const string ImageExt = ".png";
        private const string ImageOpenTag1 = "[m1] " + ImageTag;
        private const string ImageOpenTag2 = "[m2] " + ImageTag;
        private const string ImageCloseTag = ImageExt + "[/s] [/m]";

        public DicElement ParseLine(string text)
        {
            var symbol = text.Substring(0, 1);
            if (symbol == "#")
                return new DicElement(text);

            if (symbol == "{")
            {
                var number = text.Replace(@"{{Roman}}", string.Empty).Replace(@"{{/Roman}}", string.Empty).Trim();
                if (string.IsNullOrEmpty(number))
                    throw new ArgumentException("Empty entity number!");

                return new EntryNumberElement(number, text);
            }

            if (symbol == "[")
            {
                string data;
                if (GetData(text, WordFormOpenTag, CloseTag, out data))
                    return new EntryElement(EntryType.WordForm, data);

                if (GetData(text, CollocationOpenTag, CloseTag, out data))
                    return new EntryElement(EntryType.Collocation, data);

                // Simple image: replace [m1,m2] [s] ... .png[/s][/m] (but preserve .png)
                if (GetData(text, ImageOpenTag1, ImageOpenTag1.Length, ImageCloseTag, ImageCloseTag.Length - ImageExt.Length, false, out data))
                    return new EntryElement(EntryType.Image, data);
                if (GetData(text, ImageOpenTag2, ImageOpenTag2.Length, ImageCloseTag, ImageCloseTag.Length - ImageExt.Length, false, out data))
                    return new EntryElement(EntryType.Image, data);

                // Combined image: Replace [m1,m2] [s] ... [/m] (but preserve [s])
                if (GetData(text, ImageOpenTag1, ImageOpenTag1.Length - ImageTag.Length, CloseTag, CloseTag.Length, true, out data))
                    return new EntryElement(EntryType.ImageComb, data);
                if (GetData(text, ImageOpenTag2, ImageOpenTag2.Length - ImageTag.Length, CloseTag, CloseTag.Length, true, out data))
                    return new EntryElement(EntryType.ImageComb, data);

                if (GetData(text, CommentOpenTag, CloseTag, out data))
                    return new EntryElement(EntryType.Comment, data);

                if (GetData(text, EntryOpenTag, CloseTag, out data))
                    return new EntryElement(EntryType.MainEntry, data);

                throw new Exception("Unknown starting keyword: " + text);
            }

            return new KeywordElement(text, text);
        }

        private bool GetData(string source, string opentag, string endTag, out string result)
        {
            return GetData(source, opentag, opentag.Length, endTag, endTag.Length, true, out result);
        }

        private bool GetData(string source, string opentag, int openLength, string endTag, int endLength, 
            bool endTagMustExist, out string result)
        {
            result = null;
            if (!source.StartsWith(opentag, StringComparison.Ordinal))
                return false;

            if (!source.EndsWith(endTag, StringComparison.Ordinal))
            {
                if (endTagMustExist)
                    throw new Exception(string.Format("Closing tag '{0}' is missing!", endTag));

                return false;
            }

            result = source.Remove(source.Length - endLength).Remove(0, openLength).Trim();
            return true;
        }
    }
}
