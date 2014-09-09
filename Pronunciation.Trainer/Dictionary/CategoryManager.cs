using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using Pronunciation.Core.Database;
using Pronunciation.Trainer.Views;
using Pronunciation.Core.Providers.Categories;

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

        private readonly WordCategoriesCollection.WordCategoriesChangedDelegate _changeHandler;
        private readonly CategoryProvider _provider;
        private CategoriesCache _cache;

        private static readonly Guid FavoritesCategoryId = new Guid("b6eb1ab5-d0c7-487f-88fa-6e642e680ba1");

        public CategoryManager(string connectionString, 
            WordCategoriesCollection.WordCategoriesChangedDelegate changeHandler)
        {
            _changeHandler = changeHandler;
            _provider = new CategoryProvider(connectionString);
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
            var categories = LoadCategories(wordId);
            if (categories == null || categories.CategoryIds == null || categories.CategoryIds.Count == 0)
                return new WordCategoryInfo(false, null);

            return new WordCategoryInfo(
                categories.CategoryIds.Contains(FavoritesCategoryId),
                categories.CategoryIds.Where(x => x != FavoritesCategoryId).ToArray());
        }

        public DictionaryCategoryListItem[] GetAllCategories()
        {
            return _provider.GetCategories();
        }

        public int[] GetCategoryWordIds(Guid categoryId)
        {
            return _provider.GetCategoryWordIds(categoryId);
        }

        private WordCategoriesCollection LoadCategories(int wordId)
        {
            WordCategoriesCollection categories;
            if (!_cache.GetCategories(wordId, out categories))
            {
                categories = new WordCategoriesCollection(wordId, _provider);
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
