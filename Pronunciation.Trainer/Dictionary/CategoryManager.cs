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
        public delegate void WordCategoryChangedDelegate(int wordId, Guid categoryId, bool isRemoved);

        private readonly WordCategoryChangedDelegate _changeHandler;
        private readonly CategoryProvider _provider;

        public CategoryManager(string connectionString, WordCategoryChangedDelegate changeHandler)
        {
            _changeHandler = changeHandler;
            _provider = new CategoryProvider(connectionString);
        }

        public bool AddCategory(int wordId, Guid categoryId)
        {
            bool isAdded = _provider.AddWordToCategory(wordId, categoryId);
            if (isAdded && _changeHandler != null)
            {
                _changeHandler(wordId, categoryId, false);
            }

            return isAdded;
        }

        public bool RemoveCategory(int wordId, Guid categoryId)
        {
            bool isRemoved = _provider.RemoveWordFromCategory(wordId, categoryId);
            if (isRemoved && _changeHandler != null)
            {
                _changeHandler(wordId, categoryId, true);
            }

            return isRemoved;
        }

        public Guid[] GetWordCategoryIds(int wordId)
        {
            return _provider.GetWordCategoryIds(wordId);
        }

        public DictionaryCategoryItem[] GetAllCategories()
        {
            return _provider.GetCategories();
        }

        public int[] GetCategoryWordIds(Guid categoryId)
        {
            return _provider.GetCategoryWordIds(categoryId);
        }
    }
}
