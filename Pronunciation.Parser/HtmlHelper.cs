using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    class HtmlHelper
    {
        public static string PrepareAttributeValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return value.Replace("\"", "&quot;").Replace("&", "&amp;");
        }
    }
}
