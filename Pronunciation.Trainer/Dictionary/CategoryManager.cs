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
                if (_categories != null)
                {
                    _categories.Dispose();
                }
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
        private readonly WordCategoriesCollection.WordCategoriesChangedDelegate _changeHandler;
        private CategoriesCache _cache;

        private static readonly Guid FavoritesCategoryId = new Guid("b6eb1ab5-d0c7-487f-88fa-6e642e680ba1");

        public CategoryManager(WordCategoriesCollection.WordCategoriesChangedDelegate changeHandler)
        {
            _changeHandler = changeHandler;
            _dbContext = new Entities();
            _cache = new CategoriesCache();
        }

        public bool IsInFavorites(string wordName)
        {
            if (string.IsNullOrEmpty(wordName))
                return false;

            var categories = LoadCategories(wordName);
            return categories.ContainsCategory(FavoritesCategoryId);
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
            var categories = LoadCategories(wordName);
            if (categories.ContainsCategory(categoryId))
                return;

            categories.AddCategories(new[] { categoryId });
        }

        public void RemoveCategory(string wordName, Guid categoryId)
        {
            var categories = LoadCategories(wordName);
            if (categories == null || !categories.ContainsCategory(categoryId))
                return;

            categories.RemoveCategories(new[] { categoryId });
        }

        public WordCategoryInfo GetWordCategories(string wordName)
        {
            if (string.IsNullOrEmpty(wordName))
                return null;

            var categories = LoadCategories(wordName);
            var categoryIds = categories.GetCategoryIds();
            if (categoryIds != null && categoryIds.Length > 0 && categoryIds.Contains(FavoritesCategoryId))
            {
                return new WordCategoryInfo(true, categoryIds.Where(x => x != FavoritesCategoryId).ToArray());
            }
            else
            {
                return new WordCategoryInfo(false, categoryIds);
            }
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
                throw new ArgumentNullException();

            WordCategoriesCollection categories;
            if (!_cache.GetCategories(wordName, out categories))
            {
                categories = new WordCategoriesCollection(wordName);
                if (_changeHandler != null)
                {
                    categories.WordCategoriesChanged += _changeHandler;
                }
                _cache.RegisterCategories(wordName, categories);
            }

            return categories;
        }
    }
}
