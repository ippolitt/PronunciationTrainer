using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Pronunciation.Core.Providers.Categories
{
    public class DictionaryCategoryItem : IComparable<DictionaryCategoryItem>
    {
        public Guid CategoryId { get; set; }
        public string DisplayName { get; set; }
        public bool IsSystemCategory { get; set; }
        public bool IsTopCategory { get; set; }

        public DictionaryCategoryItem()
        {
        }

        public int CompareTo(DictionaryCategoryItem other)
        {
            if (other == null)
                return 1;

            if (this.IsTopCategory == other.IsTopCategory)
            {
                if (this.IsSystemCategory == other.IsSystemCategory)
                {
                    return string.Compare(this.DisplayName, other.DisplayName);
                }
                else
                {
                    return this.IsSystemCategory ? -1 : 1;
                }
            }
            else
            {
                return this.IsTopCategory ? -1 : 1;
            }
        }
    }
}
