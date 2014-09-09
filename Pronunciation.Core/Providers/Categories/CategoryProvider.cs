﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Database;
using System.Data.SqlServerCe;
using System.Data;

namespace Pronunciation.Core.Providers.Categories
{
    public class CategoryProvider
    {
        private readonly string _connectionString;

        public CategoryProvider(string connectionString)
        {
            _connectionString = connectionString;
        }

        public DictionaryCategoryListItem[] GetCategories()
        {
            var categories = new List<DictionaryCategoryListItem>();
            using (SqlCeConnection conn = new SqlCeConnection(_connectionString))
            {
                conn.Open();

                SqlCeCommand cmd = new SqlCeCommand(
@"SELECT CategoryId, DisplayName, IsSystemCategory
FROM DictionaryCategory", conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        categories.Add(new DictionaryCategoryListItem 
                        {
                            CategoryId = (Guid)reader["CategoryId"],
                            DisplayName = (string)reader["DisplayName"],
                            IsSystemCategory = reader["IsSystemCategory"] as bool?
                        });
                    }
                }
            }

            return categories.ToArray();
        }

        public int[] GetCategoryWordIds(Guid categoryId)
        {
            var wordIds = new List<int>();
            using (SqlCeConnection conn = new SqlCeConnection(_connectionString))
            {
                conn.Open();

                SqlCeCommand cmd = new SqlCeCommand(
@"SELECT WordId
FROM DictionaryCategoryWord
WHERE CategoryId = @categoryId", conn);
                cmd.Parameters.AddWithValue("@categoryId", categoryId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        wordIds.Add((int)reader["WordId"]);
                    }
                }
            }

            return wordIds.ToArray();
        }

        public Guid[] GetWordCategoryIds(int wordId)
        {
            var categoryIds = new List<Guid>();
            using (SqlCeConnection conn = new SqlCeConnection(_connectionString))
            {
                conn.Open();

                SqlCeCommand cmd = new SqlCeCommand(
@"SELECT CategoryId
FROM DictionaryCategoryWord
WHERE WordId = @wordId", conn);
                cmd.Parameters.AddWithValue("@wordId", wordId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        categoryIds.Add((Guid)reader["CategoryId"]);
                    }
                }
            }

            return categoryIds.ToArray();
        }

        public int RemoveWordFromCategories(int wordId, HashSet<Guid> categoryIds)
        {
            string idString = string.Join(", ", categoryIds.Select(x => string.Format("'{0}'", x)));
            using (SqlCeConnection conn = new SqlCeConnection(_connectionString))
            {
                conn.Open();

                SqlCeCommand cmd = new SqlCeCommand(string.Format(
@"DELETE DictionaryCategoryWord
WHERE WordId = @wordId AND CategoryId IN ({0})", idString), conn);
                cmd.Parameters.AddWithValue("@wordId", wordId);

                return cmd.ExecuteNonQuery();
            }
        }

        public void AssignWordToCategories(int wordId, HashSet<Guid> categoryIds)
        {
            using (SqlCeConnection conn = new SqlCeConnection(_connectionString))
            {
                conn.Open();

                var cmd = new SqlCeCommand(
@"INSERT DictionaryCategoryWord(CategoryId, WordId)
VALUES(@categoryId, @wordId)", conn);
                cmd.Parameters.AddWithValue("@wordId", wordId);
                var parmCategory = cmd.Parameters.Add("@categoryId", SqlDbType.UniqueIdentifier);

                foreach (var categoryId in categoryIds)
                {
                    parmCategory.Value = categoryId;
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
