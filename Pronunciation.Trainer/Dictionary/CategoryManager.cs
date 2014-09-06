using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using Pronunciation.Core.Database;
using Pronunciation.Trainer.Views;

namespace Pronunciation.Trainer.Dictionary
{
    public class CategoryManager
    {
        private class CategoriesCache
        {
            // So far we cache only categories for the latest word
            private int _latestWordId;
            private WordCategoriesCollection _categories;

            public void RegisterCategories(int wordId, WordCategoriesCollection categories)
            {
                _latestWordId = wordId;
                if (_categories != null)
                {
                    _categories.Dispose();
                }
                _categories = categories;
            }

            public bool GetCategories(int wordId, out WordCategoriesCollection categories)
            {
                if (_latestWordId == wordId)
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

        public bool IsInFavorites(int wordId)
        {
            var categories = LoadCategories(wordId);
            return categories.ContainsCategory(FavoritesCategoryId);
        }

        public void AddToFavorites(int wordId)
        {
            AddCategory(wordId, FavoritesCategoryId);
        }

        public void RemoveFromFavorites(int wordId)
        {
            RemoveCategory(wordId, FavoritesCategoryId);
        }

        public void AddCategory(int wordId, Guid categoryId)
        {
            var categories = LoadCategories(wordId);
            if (categories.ContainsCategory(categoryId))
                return;

            categories.AddCategories(new[] { categoryId });
        }

        public void RemoveCategory(int wordId, Guid categoryId)
        {
            var categories = LoadCategories(wordId);
            if (categories == null || !categories.ContainsCategory(categoryId))
                return;

            categories.RemoveCategories(new[] { categoryId });
        }

        public WordCategoryInfo GetWordCategories(int wordId)
        {
            var categoryIds = LoadCategories(wordId).GetCategoryIds();
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
            var category = _dbContext.DictionaryCategories.AsNoTracking()
                .Where(x => x.CategoryId == categoryId)
                .Include(x => x.DictionaryWords)
                .Single();
            return new HashSet<string>(category.DictionaryWords.Select(x => x.Keyword));
        }

        private WordCategoriesCollection LoadCategories(int wordId)
        {
            WordCategoriesCollection categories;
            if (!_cache.GetCategories(wordId, out categories))
            {
                categories = new WordCategoriesCollection(wordId);
                if (_changeHandler != null)
                {
                    categories.WordCategoriesChanged += _changeHandler;
                }
                _cache.RegisterCategories(wordId, categories);
            }

            return categories;
        }
    }
}
