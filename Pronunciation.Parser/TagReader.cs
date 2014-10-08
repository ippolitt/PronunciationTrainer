using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    public class ClosingTagInfo
    {
        public string TagName;
        public int PositionCorrection;

        public ClosingTagInfo(string tagName)
            : this(tagName, null)
        {
        }

        public ClosingTagInfo(string tagBaseName, string tagAddition)
        {
            TagName = tagBaseName + tagAddition;
            if (!string.IsNullOrEmpty(tagAddition))
            {
                PositionCorrection = -tagAddition.Length;
            }
        }
    }

    class TagReader
    {
        private readonly string _text;
        private readonly int _textLength;
        private readonly string[] _tagSymbols;
        private int _currentIndex;

        public string Content { get; private set; }

        public TagReader(string text) 
            : this(text, new[] { "[", "]" })
        {
        }

        public TagReader(string text, string[] tagSymbols)
        {
            _text = text;
            _textLength = text.Length;
            _currentIndex = 0;
            _tagSymbols = tagSymbols;
        }

        public int CurrentIndex
        {
            get { return _currentIndex; }
        }

        public bool IsEndOfText
        {
            get { return _currentIndex >= _textLength; }
        }

        public void ResetPosition()
        {
            _currentIndex = 0;
        }

        public bool RewindToTag(string tag)
        {
            Content = null;
            if (IsEndOfText)
                return false;

            int tagIndex = FindTag(_currentIndex, tag);
            if (tagIndex < 0)
                return false;

            _currentIndex = tagIndex + tag.Length;
            return true;
        }

        public bool IsInParentheses
        {
            get { return IsTagOpen("(", ")"); }
        }

        public bool IsTagOpen(string openingTag, string closingTag)
        {
            int openTagsCount = CalculateTagsCount(openingTag, 0, _currentIndex - 1);
            if (openTagsCount <= 0)
                return false;

            int closedTagsCount = CalculateTagsCount(closingTag, 0, _currentIndex - 1);
            return closedTagsCount != openTagsCount;
        }

        public bool LoadTagContent(string openingTag, string closingTag, bool allowInternalTags)
        {
            return LoadTagContent(openingTag, new[] { new ClosingTagInfo(closingTag) }, allowInternalTags, false);
        }

        public bool LoadTagContent(string openingTag, string[] closingTags, bool allowInternalTags)
        {
            return LoadTagContent(openingTag, closingTags.Select(x => new ClosingTagInfo(x)).ToArray(), allowInternalTags, false);
        }

        public bool LoadTagContent(string openingTag, ClosingTagInfo[] closingTags, bool allowInternalTags)
        {
            return LoadTagContent(openingTag, closingTags, allowInternalTags, false);
        }

        public bool LoadTagContent(string openingTag, string closingTag, bool allowInternalTags, bool noTagsInSkippedText)
        {
            return LoadTagContent(openingTag, new[] { new ClosingTagInfo(closingTag) }, allowInternalTags, noTagsInSkippedText);
        }

        public bool LoadTagContent(string openingTag, ClosingTagInfo[] closingTags, bool allowInternalTags, bool noTagsInSkippedText)
        {
            if (closingTags == null || closingTags.Length <= 0)
                throw new ArgumentNullException();

            Content = null;
            if (IsEndOfText)
                return false;

            int openingTagIndex = FindTag(_currentIndex, openingTag);
            if (openingTagIndex < 0)
                return false;

            if (noTagsInSkippedText)
            {
                string skippedText = GetSubstring(_currentIndex, openingTagIndex - _currentIndex);
                if (ContainsTags(skippedText))
                    return false;
            }

            int currentIndex = openingTagIndex + openingTag.Length;
            int minClosingTagIndex = -1;
            ClosingTagInfo minClosingTag = null;
            foreach (var closingTag in closingTags)
            {
                int closingTagIndex = FindTag(currentIndex, closingTag.TagName);
                if (closingTagIndex >= 0 && (minClosingTagIndex < 0 || closingTagIndex < minClosingTagIndex))
                {
                    minClosingTagIndex = closingTagIndex;
                    minClosingTag = closingTag;
                }
            }

            if (minClosingTagIndex >= 0)
            {
                string tagContent = GetSubstring(currentIndex, minClosingTagIndex - currentIndex);
                if (!allowInternalTags && ContainsTags(tagContent))
                    throw new Exception("Tag content has internal tags inside!");

                Content = tagContent;
                _currentIndex = minClosingTagIndex + minClosingTag.TagName.Length + minClosingTag.PositionCorrection;
                return true;
            }

            return false;
        }

        public bool LoadText(string upToTag)
        {
            Content = null;
            if (IsEndOfText)
                return false;

            int upToTagIndex = FindTag(_currentIndex, upToTag);
            if (upToTagIndex < 0)
                return false;

            Content = GetSubstring(_currentIndex, upToTagIndex - _currentIndex);
            _currentIndex = upToTagIndex + upToTag.Length;
            return true;
        }

        public string RemainingText
        {
            get
            {
                if (IsEndOfText)
                    return null;

                return _text.Substring(_currentIndex);
            }
        }

        public string SkippedText
        {
            get
            {
                if (_currentIndex <= 0)
                    return null;

                return _text.Substring(0, _currentIndex);
            }
        }

        private int FindTag(int startingIndex, string tag)
        {
            int tagLength = tag.Length;
            int nextIndex = startingIndex;
            while (nextIndex + tagLength <= _textLength)
            {
                if (_text.Substring(nextIndex, tagLength) == tag)
                    return nextIndex;

                nextIndex++;
            }

            return -1;
        }

        private int CalculateTagsCount(string tag, int initialPosition, int finalPosition)
        {
            int nextIndex = initialPosition;
            int tagsCount = 0;
            while (true)
            {
                nextIndex = FindTag(nextIndex, tag);
                if (nextIndex < 0 || nextIndex > finalPosition)
                    break;

                tagsCount++;
                nextIndex += tag.Length;
                if (nextIndex > finalPosition)
                    break;
            }

            return tagsCount;
        }

        private string GetSubstring(int startingIndex, int length)
        {
            if (startingIndex + length >= _textLength)
            {
                return _text.Substring(startingIndex);
            }

            return _text.Substring(startingIndex, length);
        }

        private bool ContainsTags(string text)
        {
            return string.IsNullOrEmpty(text) ? false : _tagSymbols.Any(x => text.Contains(x));
        }
    }
}
