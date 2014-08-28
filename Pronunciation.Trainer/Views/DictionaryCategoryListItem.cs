using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Pronunciation.Trainer.Views
{
    public class DictionaryCategoryListItem : IComparable<DictionaryCategoryListItem>
    {
        public Guid CategoryId { get; set; }
        public string DisplayName { get; set; }
        public bool? IsSystemCategory { get; set; }
        public bool IsServiceItem { get; set; }

        public DictionaryCategoryListItem()
        {
        }

        public override string ToString()
        {
            return DisplayName;
        }

        public int CompareTo(DictionaryCategoryListItem other)
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
