using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using Pronunciation.Core.Database;

namespace Pronunciation.Trainer.Dictionary
{
    public class WordCategoriesCollection : IDisposable
    {
        public delegate void WordCategoriesChangedDelegate(int wordId, Guid[] addedCategoryIds, Guid[] removedCategoryIds);
        public event WordCategoriesChangedDelegate WordCategoriesChanged; 

        private readonly DictionaryWord _word;
        private readonly List<DictionaryCategory> _categories;
        private Entities _dbContext;

        public WordCategoriesCollection(int wordId)
        {
            _dbContext = new Entities();
            _word = _dbContext.DictionaryWords
                .Where(x => x.WordId == wordId)
                .Include(x => x.DictionaryCategories)
                .Single();

            _categories = _word.DictionaryCategories.ToList();
        }

        public bool ContainsCategory(Guid categoryId)
        {
            return _categories.Any(x => x.CategoryId == categoryId);
        }

        public Guid[] GetCategoryIds()
        {
            return _categories.Select(x => x.CategoryId).ToArray(); 
        }

        public int RemoveCategories(IEnumerable<Guid> categoryIds)
        {
            if (_categories.Count <= 0 || categoryIds == null)
                return 0;

            var distinctIds = new HashSet<Guid>(categoryIds);
            var itemsToRemove = _categories.Where(x => distinctIds.Contains(x.CategoryId)).ToArray();
            foreach (var item in itemsToRemove)
            {
                _word.DictionaryCategories.Remove(item);
                _categories.Remove(item);
            }

            if (itemsToRemove.Length > 0)
            {
                _dbContext.SaveChanges();
                if (WordCategoriesChanged != null)
                {
                    WordCategoriesChanged(_word.WordId, null, itemsToRemove.Select(x => x.CategoryId).ToArray());
                }
            }

            return itemsToRemove.Length;
        }

        public int AddCategories(IEnumerable<Guid> categoryIds)
        {
            if (categoryIds == null)
                return 0;

            Guid[] categoriesToAdd = categoryIds.Distinct().Where(x => _categories.All(y => y.CategoryId != x)).ToArray();
            foreach (Guid categoryId in categoriesToAdd)
            {
                // This is required to avoid dbContext adding this category to the database
                DictionaryCategory category;
                var entry = _dbContext.ChangeTracker.Entries<DictionaryCategory>()
                    .FirstOrDefault(e => e.Entity.CategoryId == categoryId);
                if (entry == null)
                {
                    category = new DictionaryCategory { CategoryId = categoryId };
                    _dbContext.DictionaryCategories.Attach(category);
                }
                else
                {
                    category = entry.Entity;
                }

                _word.DictionaryCategories.Add(category);
                _categories.Add(category);
            }

            if (categoriesToAdd.Length > 0)
            {
                _dbContext.SaveChanges();
                if (WordCategoriesChanged != null)
                {
                    WordCategoriesChanged(_word.WordId, categoriesToAdd, null);
                }
            }

            return categoriesToAdd.Length;
        }

        public void Dispose()
        {
            if (_dbContext != null)
            {
                _dbContext.Dispose();
                _dbContext = null;
            }
        }
    }
}
