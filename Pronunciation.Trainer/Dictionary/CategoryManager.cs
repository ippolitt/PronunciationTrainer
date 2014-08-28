using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Database;
using Pronunciation.Trainer.Views;

namespace Pronunciation.Trainer.Dictionary
{
    public class CategoryManager
    {
        private class CategoriesCache
        {
            // So far we cache only categories for the latest word
            private string _latestWordName;
            private WordCategoriesCollection _categories;

            public void RegisterCategories(string wordName, WordCategoriesCollection categories)
            {
                _latestWordName = wordName;
                _categories = categories;
            }

            public bool GetCategories(string wordName, out WordCategoriesCollection categories)
            {
                if (_latestWordName == wordName)
                {
                    categories = _categories;
                    return true;
                }

                categories = null;
                return false;
            }
        }

        private readonly Entities _dbContext;
        private CategoriesCache _cache;

        private static readonly Guid FavoritesCategoryId = new Guid("b6eb1ab5-d0c7-487f-88fa-6e642e680ba1");

        public CategoryManager()
        {
            _dbContext = new Entities();
            _cache = new CategoriesCache();
        }

        public bool IsFavoritesCategory(DictionaryCategoryListItem category)
        {
            return category != null && category.CategoryId == FavoritesCategoryId;
        }

        public bool IsInFavorites(string wordName)
        {
            if (string.IsNullOrEmpty(wordName))
                return false;

            var categories = LoadCategories(wordName);
            return (categories == null ? false : categories.ContainsCategory(FavoritesCategoryId));
        }

        public void AddToFavorites(string wordName)
        {
            AddCategory(wordName, FavoritesCategoryId);
        }

        public void RemoveFromFavorites(string wordName)
        {
            RemoveCategory(wordName, FavoritesCategoryId);
        }

        public void AddCategory(string wordName, Guid categoryId)
        {
            if (string.IsNullOrEmpty(wordName))
                throw new ArgumentNullException();

            var categories = LoadCategories(wordName);
            if (categories == null)
            {
                categories = InitCategoriesCollection(wordName, null);
                _cache.RegisterCategories(wordName, categories);
            }
            else
            {
                if (categories.ContainsCategory(categoryId))
                    return;
            }

            categories.AddItems(new[] { new WordCategoryItem(categoryId) });
        }

        public void RemoveCategory(string wordName, Guid categoryId)
        {
            if (string.IsNullOrEmpty(wordName))
                throw new ArgumentNullException();

            var categories = LoadCategories(wordName);
            if (categories == null || !categories.ContainsCategory(categoryId))
                return;

            categories.RemoveItems(new[] { categoryId });
        }

        public WordCategoryInfo GetWordCategories(string wordName)
        {
            if (string.IsNullOrEmpty(wordName))
                return null;

            var categories = LoadCategories(wordName);
            return categories == null ? null : categories.CreateCategoryInfo();
        }

        // We allow null to be passed as WordCategoryInfo (it would mean clear all categories)
        public void UpdateWordCategories(string wordName, WordCategoryInfo info)
        {
            if (string.IsNullOrEmpty(wordName))
                throw new ArgumentNullException();

            var categories = LoadCategories(wordName);
            if (categories == null)
            {
                categories = InitCategoriesCollection(wordName, null);
                _cache.RegisterCategories(wordName, categories);
            }

            categories.UpdateFromCategoryInfo(info);
        }

        public DictionaryCategoryListItem[] GetAllCategories()
        {
            return _dbContext.DictionaryCategories.AsNoTracking()
                .Select(x => new DictionaryCategoryListItem
                {
                    CategoryId = x.CategoryId,
                    DisplayName = x.DisplayName,
                    IsSystemCategory = x.IsSystemCategory
                }).ToArray();
        }

        public HashSet<string> GetCategoryWords(Guid categoryId)
        {
            return new HashSet<string>(_dbContext.DictionaryCategoryMembers.AsNoTracking()
                .Where(x => x.CategoryId == categoryId)
                .Select(x => x.WordName));
        }

        private WordCategoriesCollection LoadCategories(string wordName)
        {
            if (string.IsNullOrEmpty(wordName))
                return null;

            WordCategoriesCollection categories;
            if (_cache.GetCategories(wordName, out categories))
                return categories;

            List<DictionaryCategoryMember> members = _dbContext.DictionaryCategoryMembers.AsNoTracking()
                .Where(x => x.WordName == wordName).ToList();
            // Remove items that don't match by case (database search is case-insensitive)
            members.RemoveAll(x => x.WordName != wordName);
            if (members.Count > 0)
            {
                categories = InitCategoriesCollection(wordName,
                    members.Select(x => new WordCategoryItem(x.CategoryId, x.MembershipId)));
            }

            // Register even NULL categories to avoid another attempt to load it from DB on a next request
            _cache.RegisterCategories(wordName, categories);
            return categories;
        }

        private WordCategoriesCollection InitCategoriesCollection(string wordName, 
            IEnumerable<WordCategoryItem> initialItems)
        {
            return new WordCategoriesCollection(wordName, FavoritesCategoryId, _dbContext, initialItems);
        }
    }
}
