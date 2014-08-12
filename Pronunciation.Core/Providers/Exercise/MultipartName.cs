using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Providers.Exercise
{
    public class MultipartName : IComparable<MultipartName>
    {
        private readonly List<NamePart> _parts = new List<NamePart>();

        // Split logic: 
        // 1.2 -> 1|2
        // 1A or 1.A -> 1|A
        // A1 or A.1 -> A|1 
        // A.1b.2 -> A|1|b|2
        public MultipartName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;

            string currentPart = null;
            bool isNumericPart = false;
            foreach(char ch in name.Trim())
            {
                if (Char.IsLetterOrDigit(ch))
                {
                    bool isDigit = Char.IsDigit(ch);
                    if (currentPart == null)
                    {
                        currentPart = ch.ToString();
                    }
                    else
                    {
                        if (isNumericPart == isDigit)
                        {
                            currentPart += ch;
                        }
                        else
                        {
                            _parts.Add(new NamePart(currentPart.Trim()));
                            currentPart = ch.ToString();
                        }
                    }

                    isNumericPart = isDigit;
                }
                else
                {
                    // Consider it as a separator
                    if (currentPart != null)
                    {
                        _parts.Add(new NamePart(currentPart.Trim()));
                        currentPart = null;
                    }
                }
            }

            if (currentPart != null)
            {
                _parts.Add(new NamePart(currentPart.Trim()));
            }
        }

        public int CompareTo(MultipartName other)
        {
            var otherParts = other._parts;

            int result = 0;
            for (int i = 0; i < _parts.Count; i++)
            {
                if (otherParts.Count <= i)
                {
                    result = 1;
                    break;
                }
                
                result = _parts[i].CompareTo(otherParts[i]);
                if (result != 0)
                    break;
            }

            if (result == 0 && otherParts.Count > _parts.Count)
            {
                result = -1;
            }

            return result;
        }

        private class NamePart : IComparable<NamePart>
        {
            private int? _number;
            private string _text;

            public NamePart(string namePart)
            {
                int number;
                if (int.TryParse(namePart, out number))
                {
                    _number = number;
                }
                _text = (namePart ?? string.Empty);
            }

            public int CompareTo(NamePart other)
            {
                if (_number.HasValue && other._number.HasValue)
                    return _number.Value.CompareTo(other._number.Value);

                return _text.CompareTo(other._text);
            }
        }
    }
}
