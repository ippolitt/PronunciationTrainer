using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Database;
using Pronunciation.Trainer.Views;

namespace Pronunciation.Trainer.Dictionary
{
    public class WordCategoryManager
    {
        private class WordMembershipCache
        {
            // So far we cache only membership for the latest word
            private string _latestWordName;
            private WordMembershipCollection _membership;

            public void RegisterMembership(string wordName, WordMembershipCollection membership)
            {
                _latestWordName = wordName;
                _membership = membership;
            }

            public bool GetMembership(string wordName, out WordMembershipCollection membership)
            {
                if (_latestWordName == wordName)
                {
                    membership = _membership;
                    return true;
                }

                membership = null;
                return false;
            }
        }

        private readonly Entities _dbContext;
        private WordMembershipCache _cache;

        private static readonly Guid FavoritesCategoryId = new Guid("b6eb1ab5-d0c7-487f-88fa-6e642e680ba1");

        public WordCategoryManager()
        {
            _dbContext = new Entities();
            _cache = new WordMembershipCache();
        }

        public bool IsFavoritesCategory(WordCategoryListItem category)
        {
            return category != null && category.WordCategoryId == FavoritesCategoryId;
        }

        public bool IsInFavorites(string wordName)
        {
            if (string.IsNullOrEmpty(wordName))
                return false;

            var membership = LoadMembership(wordName);
            return (membership == null ? false : membership.ContainsFavorites);
        }

        public void AddToFavorites(string wordName)
        {
            if (string.IsNullOrEmpty(wordName))
                throw new ArgumentNullException();

            var membership = LoadMembership(wordName);
            if (membership == null)
            {
                membership = InitMembershipCollection(wordName, null);
                _cache.RegisterMembership(wordName, membership);
            }
            else
            {
                if (membership.ContainsFavorites)
                    return;
            }

            membership.AddItems(new[] { new WordMembershipItem(FavoritesCategoryId) });
        }

        public void RemoveFromFavorites(string wordName)
        {
            if (string.IsNullOrEmpty(wordName))
                throw new ArgumentNullException();

            var membership = LoadMembership(wordName);
            if (membership == null || !membership.ContainsFavorites)
                return;

            membership.RemoveItems(new[] { FavoritesCategoryId });
        }

        public WordCategoryInfo GetWordCategories(string wordName)
        {
            if (string.IsNullOrEmpty(wordName))
                return null;

            var membership = LoadMembership(wordName);
            return membership == null ? null : membership.CreateCategoryInfo();
        }

        // We allow null to be passed as WordCategoryInfo (it would mean clear all categories)
        public void UpdateWordCategories(string wordName, WordCategoryInfo info)
        {
            if (string.IsNullOrEmpty(wordName))
                throw new ArgumentNullException();

            var membership = LoadMembership(wordName);
            if (membership == null)
            {
                membership = InitMembershipCollection(wordName, null);
                _cache.RegisterMembership(wordName, membership);
            }

            membership.UpdateFromCategoryInfo(info);
        }

        public WordCategoryListItem[] GetAllCategories()
        {
            return _dbContext.WordCategories.AsNoTracking()
                .Select(x => new WordCategoryListItem
                {
                    WordCategoryId = x.WordCategoryId,
                    DisplayName = x.DisplayName,
                    IsSystemCategory = x.IsSystemCategory
                }).ToArray();
        }

        public HashSet<string> GetCategoryWords(Guid categoryId)
        {
            return new HashSet<string>(_dbContext.WordCategoryMemberships.AsNoTracking()
                .Where(x => x.WordCategoryId == categoryId)
                .Select(x => x.WordName));
        }

        private WordMembershipCollection LoadMembership(string wordName)
        {
            if (string.IsNullOrEmpty(wordName))
                return null;

            WordMembershipCollection membership;
            if (_cache.GetMembership(wordName, out membership))
                return membership;

            List<WordCategoryMembership> categories = _dbContext.WordCategoryMemberships.AsNoTracking()
                .Where(x => x.WordName == wordName).ToList();
            // Remove items that don't match by case (database search is case-insensitive)
            categories.RemoveAll(x => x.WordName != wordName);
            if (categories.Count > 0)
            {
                membership = InitMembershipCollection(wordName,
                    categories.Select(x => new WordMembershipItem(x.WordCategoryId, x.MembershipId)));
            }

            // Register even NULL membership to avoid another attempt to load it from DB on a next request
            _cache.RegisterMembership(wordName, membership);
            return membership;
        }

        private WordMembershipCollection InitMembershipCollection(string wordName, 
            IEnumerable<WordMembershipItem> initialItems)
        {
            return new WordMembershipCollection(wordName, FavoritesCategoryId, _dbContext, initialItems);
        }
    }
}
