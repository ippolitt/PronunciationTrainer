using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using Pronunciation.Core.Database;
using Pronunciation.Core.Providers.Categories;

namespace Pronunciation.Trainer.Dictionary
{
    public class WordCategoriesCollection
    {
        public delegate void WordCategoriesChangedDelegate(int wordId, Guid[] addedCategoryIds, Guid[] removedCategoryIds);
        public event WordCategoriesChangedDelegate WordCategoriesChanged; 

        private readonly int _wordId;
        private readonly CategoryProvider _provider;
        private readonly HashSet<Guid> _categoryIds;

        public WordCategoriesCollection(int wordId, CategoryProvider provider)
        {
            _wordId = wordId;
            _provider = provider;
            _categoryIds = new HashSet<Guid>(provider.GetWordCategoryIds(wordId));
        }

        public bool ContainsCategory(Guid categoryId)
        {
            return _categoryIds.Any(x => x == categoryId);
        }

        public HashSet<Guid> CategoryIds
        {
            get { return _categoryIds; }
        }

        public int RemoveCategories(IEnumerable<Guid> categoryIds)
        {
            if (_categoryIds.Count <= 0 || categoryIds == null)
                return 0;

            var idsToRemove = new HashSet<Guid>(categoryIds.Where(x => _categoryIds.Contains(x)));
            if (idsToRemove.Count > 0)
            {
                _provider.RemoveWordFromCategories(_wordId, idsToRemove);
                foreach (var id in idsToRemove)
                {
                    _categoryIds.Remove(id);
                }

                if (WordCategoriesChanged != null)
                {
                    WordCategoriesChanged(_wordId, null, idsToRemove.ToArray());
                }
            }

            return idsToRemove.Count;
        }

        public int AddCategories(IEnumerable<Guid> categoryIds)
        {
            if (categoryIds == null)
                return 0;

            var idsToAdd = new HashSet<Guid>(categoryIds.Where(x => !_categoryIds.Contains(x)));
            if (idsToAdd.Count > 0)
            {
                _provider.AssignWordToCategories(_wordId, idsToAdd);
                foreach (var id in idsToAdd)
                {
                    _categoryIds.Add(id);
                }

                if (WordCategoriesChanged != null)
                {
                    WordCategoriesChanged(_wordId, idsToAdd.ToArray(), null);
                }
            }

            return idsToAdd.Count;
        }
    }
}
