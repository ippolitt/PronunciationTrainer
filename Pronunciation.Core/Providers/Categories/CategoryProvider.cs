using System;
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

        public DictionaryCategoryItem[] GetCategories()
        {
            var categories = new List<DictionaryCategoryItem>();
            using (SqlCeConnection conn = new SqlCeConnection(_connectionString))
            {
                conn.Open();

                SqlCeCommand cmd = new SqlCeCommand(
@"SELECT CategoryId, DisplayName, IsSystemCategory, IsTopCategory
FROM DictionaryCategory", conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        categories.Add(new DictionaryCategoryItem 
                        {
                            CategoryId = (Guid)reader["CategoryId"],
                            DisplayName = (string)reader["DisplayName"],
                            IsSystemCategory = (reader["IsSystemCategory"] as bool?) == true,
                            IsTopCategory = (reader["IsTopCategory"] as bool?) == true
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

        public bool RemoveWordFromCategory(int wordId, Guid categoryId)
        {
            using (SqlCeConnection conn = new SqlCeConnection(_connectionString))
            {
                conn.Open();

                SqlCeCommand cmd = new SqlCeCommand(
@"DELETE DictionaryCategoryWord
WHERE CategoryId = @categoryId AND WordId = @wordId", 
                    conn);
                cmd.Parameters.AddWithValue("@categoryId", categoryId);
                cmd.Parameters.AddWithValue("@wordId", wordId);

                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool AddWordToCategory(int wordId, Guid categoryId)
        {
            using (SqlCeConnection conn = new SqlCeConnection(_connectionString))
            {
                conn.Open();

                var cmd = new SqlCeCommand(string.Format(
@"INSERT INTO DictionaryCategoryWord (CategoryId, WordId)
SELECT '{0}' AS CategoryId, {1} AS WordId
WHERE NOT EXISTS (SELECT * FROM DictionaryCategoryWord WHERE CategoryId = '{0}' AND WordId = '{1}')", 
                    categoryId, wordId), conn);
                // For some reason, SQL CE doesn't understand parameters in SELECT statement

                return cmd.ExecuteNonQuery() > 0;
            }
        }
    }
}
