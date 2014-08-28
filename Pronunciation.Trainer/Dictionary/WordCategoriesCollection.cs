using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Database;

namespace Pronunciation.Trainer.Dictionary
{
    public class WordCategoriesCollection
    {
        private readonly List<WordCategoryItem> _items;
        private readonly Guid _favoritesCategoryId;
        private readonly Entities _dbContext;
        private readonly string _wordName;

        private readonly static EntityComparer _comparer = new EntityComparer();

        public WordCategoriesCollection(string wordName, Guid favoritesCategoryId,
            Entities dbContext, IEnumerable<WordCategoryItem> initialItems)
        {
            _wordName = wordName;
            _favoritesCategoryId = favoritesCategoryId;
            _dbContext = dbContext;
            if (initialItems == null)
            {
                _items = new List<WordCategoryItem>();
            }
            else
            {
                _items = new List<WordCategoryItem>(initialItems);
            }
        }

        public bool ContainsCategory(Guid categoryId)
        {
            return _items.Any(x => x.CategoryId == categoryId);
        }

        public WordCategoryInfo CreateCategoryInfo()
        {
            return new WordCategoryInfo
            {
                IsInFavorites = _items.Any(x => x.CategoryId == _favoritesCategoryId),
                Categories = _items.Where(x => x.CategoryId != _favoritesCategoryId).Select(x => x.CategoryId).ToList()
            };
        }

        public int RemoveItems(IEnumerable<Guid> categoryIds)
        {
            if (_items.Count <= 0 || categoryIds == null)
                return 0;

            var distinctIds = new HashSet<Guid>(categoryIds);
            var itemsToRemove = new List<WordCategoryItem>(_items.Where(x => distinctIds.Contains(x.CategoryId)));
            return StoreItems(itemsToRemove, null);
        }

        public int AddItems(IEnumerable<WordCategoryItem> newItems)
        {
            if (newItems == null)
                return 0;

            var distinctItems = newItems.Distinct(_comparer).ToArray();
            var itemsToAdd = new List<WordCategoryItem>(distinctItems.Where(x => !_items.Contains(x, _comparer)));
            return StoreItems(null, itemsToAdd);
        }

        public void UpdateFromCategoryInfo(WordCategoryInfo info)
        {
            var itemsToRemove = new List<WordCategoryItem>();
            var itemsToAdd = new List<WordCategoryItem>();
            if (info == null)
            {
                itemsToRemove.AddRange(_items);
            }
            else
            {
                var distinctIds = info.Categories == null ? new HashSet<Guid>() : new HashSet<Guid>(info.Categories);
                if (info.IsInFavorites)
                {
                    distinctIds.Add(_favoritesCategoryId);
                }

                itemsToRemove.AddRange(_items.Where(x => distinctIds.Contains(x.CategoryId)));
                itemsToAdd.AddRange(distinctIds.Where(x => _items.All(y => y.CategoryId != x))
                    .Select(x => new WordCategoryItem(x)));
            }

            StoreItems(itemsToRemove, itemsToAdd);
        }

        private int StoreItems(List<WordCategoryItem> itemsToRemove, List<WordCategoryItem> itemsToAdd)
        {
            int removeCount = itemsToRemove == null ? 0 : itemsToRemove.Count;       
            int addCount = itemsToAdd == null ? 0 : itemsToAdd.Count;
            if (removeCount > 0 || addCount > 0)
            {
                // Important: removed items should be processed first!
                if (removeCount > 0)
                {
                    RemoveDatabaseMembers(itemsToRemove);
                }
                if (addCount > 0)
                {
                    AddDatabaseMembers(itemsToAdd);
                }

                // Save all changes in one call
                _dbContext.SaveChanges();

                if (removeCount > 0)
                {
                    foreach (var item in itemsToRemove)
                    {
                        _items.Remove(item);
                    }
                }
                if (addCount > 0)
                {
                    _items.AddRange(itemsToAdd);
                }
            }

            return addCount + removeCount;
        }

        private void AddDatabaseMembers(IEnumerable<WordCategoryItem> items)
        {
            foreach (var item in items)
            {
                DictionaryCategoryMember member = _dbContext.DictionaryCategoryMembers.Create();
                member.MembershipId = item.MembershipId;
                member.WordName = _wordName;
                member.CategoryId = item.CategoryId;
                _dbContext.DictionaryCategoryMembers.Add(member);
            }
        }

        private void RemoveDatabaseMembers(IEnumerable<WordCategoryItem> items)
        {
            foreach (var item in items)
            {
                DictionaryCategoryMember entity;
                var entry = _dbContext.ChangeTracker.Entries<DictionaryCategoryMember>()
                    .FirstOrDefault(e => e.Entity.MembershipId == item.MembershipId);
                if (entry == null)
                {
                    entity = new DictionaryCategoryMember { MembershipId = item.MembershipId };
                    _dbContext.DictionaryCategoryMembers.Attach(entity);
                }
                else
                {
                    entity = entry.Entity;
                }
                _dbContext.DictionaryCategoryMembers.Remove(entity);
            } 
        }

        private class EntityComparer : IEqualityComparer<WordCategoryItem>
        {
            public bool Equals(WordCategoryItem x, WordCategoryItem y)
            {
                if (x != null && y != null)
                {
                    return x.CategoryId == y.CategoryId;
                }
                else
                {
                    return (x == null && y == null);
                }
            }

            public int GetHashCode(WordCategoryItem obj)
            {
                return obj == null ? 0 : obj.CategoryId.GetHashCode();
            }
        }
    }
}
