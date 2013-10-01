using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core
{
    public class KeyTextPair<TKey>
    {
        public TKey Key { get; private set; }
        public string Text { get; private set; }

        public override string ToString()
        {
            return Text;
        }

        public KeyTextPair()
        {
        }

        public KeyTextPair(TKey key)
        {
            Key = key;
            Text = key.ToString();
        }

        public KeyTextPair(TKey key, string text)
        {
            Key = key;
            Text = text;
        }
    }
}
