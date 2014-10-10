using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Trainer.Views;
using Pronunciation.Trainer.Utility;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Pronunciation.Core.Providers.Categories;

namespace Pronunciation.Trainer.Dictionary
{
    public class WordCategoryStateTracker
    {
        private readonly List<WordCategoryListItem> _categories;
        private readonly CategoryManager _categoryManager;
        private readonly IgnoreEventsRegion _ignoreEvents = new IgnoreEventsRegion();
        private int? _currentWordId;

        public WordCategoryStateTracker(CategoryManager categoryManager)
        {
            _categoryManager = categoryManager;
            _categories = new List<WordCategoryListItem>();
        }

        public int? CurrentWordId
        {
            get { return _currentWordId; }
        }

        public void SynchronizeCategories(IEnumerable<DictionaryCategoryListItem> categories)
        {
            using (var region = _ignoreEvents.Start())
            {
                var categoryIds = new HashSet<Guid>();
                foreach(var category in categories.Where(x => !(x.IsSystemCategory == true || x.IsServiceItem)))
                {
                    var item = _categories.SingleOrDefault(x => x.CategoryId == category.CategoryId);
                    if (item == null)
                    {
                        item = new WordCategoryListItem(category.CategoryId, category.DisplayName);
                        item.PropertyChanged += WordCategory_PropertyChanged;
                        _categories.Add(item);
                    }
                    else
                    {
                        item.DisplayName = category.DisplayName;
                    }

                    categoryIds.Add(category.CategoryId);
                }

                _categories.RemoveAll(x => !categoryIds.Contains(x.CategoryId));
                _categories.Sort(Compare);
            }
        }

        public ObservableCollection<WordCategoryListItem> GetCategories()
        {
            return new ObservableCollection<WordCategoryListItem>(_categories);
        }

        public void RegisterWord(int wordId)
        {
            _currentWordId = wordId;
            Guid[] wordCategoryIds = _categoryManager.GetWordCategoryIds(wordId);

            using (var region = _ignoreEvents.Start())
            {
                if (wordCategoryIds == null || wordCategoryIds.Length == 0)
                {
                    _categories.ForEach(x => x.IsAssigned = false);
                }
                else
                {
                    _categories.ForEach(x => x.IsAssigned = wordCategoryIds.Contains(x.CategoryId));
                }
            }
        }

        public void ResetWord()
        {
            _currentWordId = null;
            using (var region = _ignoreEvents.Start())
            {
                _categories.ForEach(x => x.IsAssigned = false);
            }
        }

        public bool RefreshCategoryState(Guid categoryId, bool isAssigned)
        {
            var category = _categories.SingleOrDefault(x => x.CategoryId == categoryId);
            if (category == null)
                return false;

            if (category.IsAssigned == isAssigned)
                return true;

            using (var region = _ignoreEvents.Start())
            {
                category.IsAssigned = isAssigned;
            }

            return true;
        }

        private void WordCategory_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_currentWordId == null || _ignoreEvents.IsActive)
                return;

            if ((sender is WordCategoryListItem) && e.PropertyName == WordCategoryListItem.IsAssignedPropertyName)
            {
                var item = (WordCategoryListItem)sender;
                if (item.IsAssigned)
                {
                    _categoryManager.AddCategory(_currentWordId.Value, item.CategoryId);
                }
                else
                {
                    _categoryManager.RemoveCategory(_currentWordId.Value, item.CategoryId);
                }
            }
        }

        private int Compare(WordCategoryListItem x, WordCategoryListItem y)
        {
            if (x != null && y != null)
            {
                return string.Compare(x.DisplayName, y.DisplayName);
            }
            else
            {
                return (x == null && y == null) ? 0 : (x == null ? -1 : 1); 
            }
        }
    }
}
