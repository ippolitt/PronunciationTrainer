using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Pronunciation.Core.Utility
{
    public static class HtmlHelper
    {
        public static string PrepareHtmlContent(string content)
        {
            return PrepareHtmlContent(content, false);
        }

        public static string PrepareHtmlContent(string content, bool insertLinebreak)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            var result = HttpUtility.HtmlEncode(content);
            if (insertLinebreak)
            {
                result = result.Replace(Environment.NewLine, "<BR>");
            }

            return result;
        }
    }
}
