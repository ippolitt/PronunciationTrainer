using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Pronunciation.Trainer.Views
{
    public class WordCategoryListItem : IComparable<WordCategoryListItem>
    {
        public Guid WordCategoryId { get; set; }
        public string DisplayName { get; set; }
        public bool? IsSystemCategory { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }

        public int CompareTo(WordCategoryListItem other)
        {
            if (other == null)
                return 1;
            
            if ((this.IsSystemCategory ?? false) == (other.IsSystemCategory ?? false))
            {
                return string.Compare(this.DisplayName, other.DisplayName);
            }
            else
            {
                return (this.IsSystemCategory == true) ? -1 : 1;
            }
        }
    }
}
