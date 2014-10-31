using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

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

        public static string PrepareJScriptString(string value, bool allowSingleQuote)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            if (allowSingleQuote)
            {
                value = value.Replace(@"""", @"\""");
            }
            else
            {
                value = value.Replace("'", @"\'");
            }

            return value;
        }
    }
}
