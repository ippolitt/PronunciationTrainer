using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Providers.Categories;

namespace Pronunciation.Trainer.Views
{
    public class DictionaryCategoryListItem
    {
        public bool IsServiceItem { get; private set; }
        public bool IsSeparator { get; private set; }

        private readonly DictionaryCategoryItem _target;
        private readonly string _displayName;

        public DictionaryCategoryListItem()
        {
            IsServiceItem = true;
            IsSeparator = true;
        }

        public DictionaryCategoryListItem(string displayName)
        {
            _displayName = displayName;
            IsServiceItem = true;
        }

        public DictionaryCategoryListItem(DictionaryCategoryItem target)
        {
            _target = target;
        }

        public override string ToString()
        {
            return DisplayName;
        }

        public Guid CategoryId 
        {
            get { return _target == null ? Guid.Empty : _target.CategoryId; } 
        }

        public string DisplayName
        {
            get { return _target == null ? _displayName : _target.DisplayName; }
        }
    }
}
