using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    // Returns upper-case words first
    class UppercaseFirstComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (!string.IsNullOrEmpty(x) && !string.IsNullOrEmpty(y))
            {
                if (Char.IsUpper(x[0]) && Char.IsLower(y[0]))
                    return -1;

                if (Char.IsLower(x[0]) && Char.IsUpper(y[0]))
                    return 1;
            }

            return string.Compare(x, y);
        }
    }
}
