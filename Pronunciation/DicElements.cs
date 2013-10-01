using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation
{
    public enum EntryType
    {
        Keyword,
        MainEntry,
        Comment,
        WordForm,
        Collocation,
        Image,
        ImageComb
    }

    public class DicElement
    {
        public string Text { get; private set; }

        public DicElement(string text)
        {
            Text = text;
        }
    }

    public class KeywordElement : DicElement
    {
        public string Keyword { get; private set; }

        public KeywordElement(string keyword, string text)
            : base(text)
        {
            Keyword = keyword;
        }
    }

    public class EntryElement : DicElement
    {
        public EntryType EntryType { get; private set; }

        public EntryElement(EntryType entryType, string text)
            : base(text)
        {
            EntryType = entryType;
        }
    }

    public class EntryNumberElement : DicElement
    {
        public string Number { get; private set; }

        public EntryNumberElement(string number, string text)
            : base(text)
        {
            Number = number;
        }
    }
}
