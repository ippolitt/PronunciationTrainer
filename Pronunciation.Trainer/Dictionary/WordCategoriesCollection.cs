using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Database;

namespace Pronunciation.Trainer.Dictionary
{
    public class WordCategoriesCollection : IDisposable
    {
        public delegate void WordCategoriesChangedDelegate(string wordName, Guid[] addedCategoryIds, Guid[] removedCategoryIds);
        public event WordCategoriesChangedDelegate WordCategoriesChanged; 

        private readonly List<DictionaryCategoryMember> _items;
        private readonly string _wordName;
        private Entities _dbContext;

        public WordCategoriesCollection(string wordName)
        {
            _wordName = wordName;
            _dbContext = new Entities();
            _items = _dbContext.DictionaryCategoryMembers.Where(x => x.WordName == wordName).ToList();

            // Remove items that don't match by case (database search is case-insensitive)
            _items.RemoveAll(x => x.WordName != wordName);
        }

        public bool ContainsCategory(Guid categoryId)
        {
            return _items.Any(x => x.CategoryId == categoryId);
        }

        public Guid[] GetCategoryIds()
        {
            return _items.Select(x => x.CategoryId).ToArray();
        }

        public int RemoveCategories(IEnumerable<Guid> categoryIds)
        {
            if (_items.Count <= 0 || categoryIds == null)
                return 0;

            var distinctIds = new HashSet<Guid>(categoryIds);
            DictionaryCategoryMember[] itemsToRemove = _items.Where(x => distinctIds.Contains(x.CategoryId)).ToArray();
            foreach (var item in itemsToRemove)
            {
                _dbContext.DictionaryCategoryMembers.Remove(item);
                _items.Remove(item);
            }

            if (itemsToRemove.Length > 0)
            {
                _dbContext.SaveChanges();
                if (WordCategoriesChanged != null)
                {
                    WordCategoriesChanged(_wordName, null, itemsToRemove.Select(x => x.CategoryId).ToArray());
                }
            }

            return itemsToRemove.Length;
        }

        public int AddCategories(IEnumerable<Guid> categoryIds)
        {
            if (categoryIds == null)
                return 0;

            Guid[] categoriesToAdd = categoryIds.Distinct().Where(x => _items.All(y => y.CategoryId != x)).ToArray();
            foreach (Guid categoryId in categoriesToAdd)
            {
                DictionaryCategoryMember item = _dbContext.DictionaryCategoryMembers.Create();
                item.MembershipId = Guid.NewGuid();
                item.WordName = _wordName;
                item.CategoryId = categoryId;

                _dbContext.DictionaryCategoryMembers.Add(item);
                _items.Add(item);
            }

            if (categoriesToAdd.Length > 0)
            {
                _dbContext.SaveChanges();
                if (WordCategoriesChanged != null)
                {
                    WordCategoriesChanged(_wordName, categoriesToAdd, null);
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
