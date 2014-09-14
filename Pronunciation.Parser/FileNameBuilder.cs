using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Pronunciation.Parser
{
    class FileNameBuilder
    {
        private static List<string> _forbiddenNames;
        private static Dictionary<char, string> _wordNameMap;

        private static Regex _htmlTagsRegex = new Regex("<.*?>", RegexOptions.Compiled);
        private static Regex _wordNameRegex = new Regex("[A-Za-z0-9 _'&+.-]", RegexOptions.Compiled);
        private static Regex _firstLetterRegex = new Regex("[A-Za-z0-9]", RegexOptions.Compiled);

        static FileNameBuilder()
        {
            _wordNameMap = new Dictionary<char, string>();
            _wordNameMap.Add('/', "-");
            _wordNameMap.Add('à', "a");
            _wordNameMap.Add('á', "a");
            _wordNameMap.Add('å', "a");
            _wordNameMap.Add('â', "a");
            _wordNameMap.Add('ä', "a");
            _wordNameMap.Add('ã', "a");
            _wordNameMap.Add('é', "e");
            _wordNameMap.Add('è', "e");
            _wordNameMap.Add('ë', "e");
            _wordNameMap.Add('ê', "e");
            _wordNameMap.Add('É', "E");
            _wordNameMap.Add('Æ', "ae");
            _wordNameMap.Add('ó', "o");
            _wordNameMap.Add('ö', "o");
            _wordNameMap.Add('ô', "o");
            _wordNameMap.Add('ø', "o");
            _wordNameMap.Add('Ó', "O");
            _wordNameMap.Add('Ö', "O");
            _wordNameMap.Add('ç', "c");
            _wordNameMap.Add('č', "c");
            _wordNameMap.Add('Č', "C");
            _wordNameMap.Add('š', "s");
            _wordNameMap.Add('ü', "u");
            _wordNameMap.Add('û', "u");
            _wordNameMap.Add('ů', "u");
            _wordNameMap.Add('ñ', "n");
            _wordNameMap.Add('í', "i");
            _wordNameMap.Add('î', "i");

            // These names are borbidden in Windows OS
            _forbiddenNames = new List<string>();
            _forbiddenNames.AddRange(
                "CON, PRN, AUX, NUL, COM1, COM2, COM3, COM4, COM5, COM6, COM7, COM8, COM9, LPT1, LPT2, LPT3, LPT4, LPT5, LPT6, LPT7, LPT8, LPT9"
                .ToLower().Split(new[] { ',', ' ' }));
        }

        public FileNameBuilder()
        {
        }

        public string PrepareFileName(string keyword, StringBuilder stat)
        {
            var text = _htmlTagsRegex.Replace(keyword, string.Empty).Replace("&amp;", "&");
            // TODO: after migration to DB uncomment this code for generating html files ???
            //if (text.EndsWith("..."))
            //{
            //    text = text.Remove(text.Length - 3);
            //    if (stat != null)
            //    {
            //        stat.AppendFormat("Ignored '...' at the end of [{0}] word\r\n", keyword);
            //    }
            //}
            //if (text.EndsWith("-"))
            //{
            //    text = text.Remove(text.Length - 1);
            //    if (stat != null)
            //    {
            //        stat.AppendFormat("Ignored '-' at the end of [{0}] word\r\n", keyword);
            //    }
            //}

            var bld = new StringBuilder();
            Regex regex;
            foreach (var ch in text)
            {
                if (bld.Length == 0)
                {
                    regex = _firstLetterRegex;
                }
                else
                {
                    regex = _wordNameRegex;
                }

                if (regex.IsMatch(ch.ToString()))
                {
                    bld.Append(ch);
                }
                else
                {
                    string replacement;
                    if (_wordNameMap.TryGetValue(ch, out replacement))
                    {
                        if (regex.IsMatch(replacement))
                        {
                            bld.Append(replacement);
                        }
                        else
                        {
                            if (stat != null)
                            {
                                stat.AppendFormat("Ignored [{0}] and replacement [{1}] in [{2}]\r\n", ch, replacement, keyword);
                            }
                        }
                    }
                    else
                    {
                        if (stat != null)
                        {
                            stat.AppendFormat("Ignored [{0}] in [{1}]\r\n", ch, keyword);
                        }
                    }
                }
            }

            var result = bld.ToString().ToLower();

            if (_forbiddenNames.Contains(result))
            {
                if (stat != null)
                {
                    stat.AppendFormat("Using [_{0}] instead of [{0}] (forbidden OS name)\r\n", result);
                }
                return "_" + result;
            }

            return result;
        }
    }
}
