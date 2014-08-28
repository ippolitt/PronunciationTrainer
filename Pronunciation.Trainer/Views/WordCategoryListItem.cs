using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Trainer.Dictionary;
using System.ComponentModel;

namespace Pronunciation.Trainer.Views
{
    public class WordCategoryListItem : INotifyPropertyChanged
    {
        public Guid CategoryId { get; private set; }
        public const string IsAssignedPropertyName = "IsAssigned";
        public const string DisplayNamePropertyName = "DisplayName";

        public event PropertyChangedEventHandler PropertyChanged;

        private bool _isAssigned;
        private string _displayName;

        public WordCategoryListItem(Guid categoryId, string displayName)
        {
            CategoryId = categoryId;
            _displayName = displayName;
        }

        public string DisplayName
        {
            get
            {
                return _displayName;
            }
            set
            {
                if (_displayName == value)
                    return;

                _displayName = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(DisplayNamePropertyName));
                }
            }
        }

        public bool IsAssigned 
        { 
            get 
            { 
                return _isAssigned; 
            }
            set 
            {
                if (_isAssigned == value)
                    return;

                _isAssigned = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(IsAssignedPropertyName));
                }
            }
        }
    }
}
