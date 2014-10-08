using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    static class StringHelper
    {
        public static bool StartsWithOrdinal(this string target, string value)
        {
            return StartsWithOrdinal(target, value, false);
        }

        public static bool StartsWithOrdinal(this string target, string value, bool ignoreCase)
        {
            return target.StartsWith(value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }
    }
}
