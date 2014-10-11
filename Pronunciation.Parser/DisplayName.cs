using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    public class DisplayName
    {
        private readonly static UppercaseFirstComparer _comparer = new UppercaseFirstComparer();

        private readonly List<string> _titles;
        private bool _joinWithSemicolon;

        private DisplayName(IEnumerable<string> titles)
        {
            _titles = new List<string>(titles);
            _joinWithSemicolon = _titles.Any(x => x.Contains(","));
        }

        private DisplayName(List<string> titles, bool joinWithSemicolon)
        {
            _titles = titles;
            _joinWithSemicolon = joinWithSemicolon;
        }

        public DisplayName(string title)
        {
            _titles = new List<string>();
            Append(title);
        }

        public override string ToString()
        {
            if (_titles.Count == 0)
                return null;

            return string.Join((_joinWithSemicolon ? "; " : ", "), _titles);
        }

        public string GetStringWithoutStress()
        {
            return string.Join((_joinWithSemicolon ? "; " : ", "), _titles.Select(RemoveStress));
        }

        public string Serialize()
        {
            if (_titles.Count == 0)
                return null;

            return string.Join(";", _titles);
        }

        public static DisplayName Deserialize(string serializedString)
        {
            if (string.IsNullOrEmpty(serializedString))
                return null;

            return new DisplayName(serializedString.Split(';'));
        }

        public void Append(string title)
        {
            if (string.IsNullOrEmpty(title))
                return;

            string sourceWithoutStress = RemoveStress(title);
            bool isSourceUppercase = Char.IsUpper(sourceWithoutStress[0]);
            int insertIndex = -1;
            for (int i = 0; i < _titles.Count; i++)
            {
                if (_titles[i] == title)
                    return;

                string targetWithoutStress = RemoveStress(_titles[i]);
                if (targetWithoutStress == sourceWithoutStress)
                {
                    // Source title doesn't have a stress so we ignore it
                    if (title.Length == sourceWithoutStress.Length)
                        return;

                    // Target item doesn't have a stress so we replace it with the title with stress
                    if (_titles[i].Length == targetWithoutStress.Length)
                    {
                        _titles[i] = title;
                        return;
                    }

                    // Both items have stress and it differs - new title should be added
                }

                if (isSourceUppercase && insertIndex < 0)
                {
                    // Find first lowercase title and insert new title just before it
                    if (Char.IsLower(targetWithoutStress[0]))
                    {
                        insertIndex = i;
                    }
                }
            }

            if (insertIndex >= 0)
            {
                _titles.Insert(insertIndex, title);
            }
            else
            {
                _titles.Add(title);
            }
            
            if (title.Contains(","))
            {
                _joinWithSemicolon = true;
            }
        }

        public void Merge(DisplayName source)
        {
            if (source == null)
                return;

            foreach (var title in source._titles)
            {
                Append(title);
            }
        }

        public DisplayName Clone()
        {
            return new DisplayName(new List<string>(_titles), _joinWithSemicolon);
        }

        public bool IsEqual(DisplayName title)
        {
            if (title == null)
                return false;

            return GetStringWithoutStress() == title.GetStringWithoutStress();
        }

        public bool ContainsStress
        {
            get { return _titles.Any(x => x.Contains("ˈ") || x.Contains("ˌ")); }
        }

        private string RemoveStress(string title)
        {
            if (string.IsNullOrEmpty(title))
                return title;

            return title.Replace("ˈ", "").Replace("ˌ", "").Trim();
        }
    }
}
