using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Providers
{
    public class IndexEntry
    {
        public string PageKey { get; private set; }
        public string Text { get; private set; }
        public bool IsCollocation { get; private set; }
        public string SoundKeyUK { get; private set; }
        public string SoundKeyUS { get; private set; }

        public override string ToString()
        {
            return Text;
        }

        public IndexEntry(string pageKey, string text, bool isCollocation)
        {
            PageKey = pageKey;
            Text = text;
            IsCollocation = isCollocation;
        }

        public IndexEntry(string pageKey, string text, bool isCollocation, string soundKeyUK, string soundKeyUS)
            : this(pageKey, text, isCollocation)
        {
            SoundKeyUK = soundKeyUK;
            SoundKeyUS = soundKeyUS;
        }
    }
}
