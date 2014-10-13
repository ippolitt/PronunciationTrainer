using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Providers.Dictionary;

namespace Pronunciation.Trainer.Dictionary
{
    public class AutoListsManager
    {
        public readonly static Guid WordsWithNotes = new Guid("b3caba42-b4b0-4d58-a820-f976d2f8dc28");
        public readonly static Guid WordsWithMultiplePronunciations = new Guid("7b0311a1-e695-433a-b43a-e9fbe492f615");
        private readonly static Guid[] AutoListIds;

        private readonly IDictionaryProvider _provider;

        static AutoListsManager()
        {
            AutoListIds = new Guid[] { WordsWithNotes, WordsWithMultiplePronunciations };
        }

        public AutoListsManager(IDictionaryProvider provider)
        {
            _provider = provider;
        }

        public bool IsAutoList(Guid listId)
        {
             return AutoListIds.Contains(listId);
        }

        public IEnumerable<IndexEntry> ApplyAutoList(Guid listId, IEnumerable<IndexEntry> query)
        {
            bool noRecords = false;
            if (listId == WordsWithNotes)
            {
                List<int> wordIds = _provider.GetWordsWithNotes();
                if (wordIds == null || wordIds.Count == 0)
                {
                    noRecords = true;
                }
                else
                {
                    HashSet<int> ids = new HashSet<int>(wordIds);
                    query = query.Where(x => x.WordId != null && ids.Contains(x.WordId.Value));
                }
            }
            else if (listId == WordsWithMultiplePronunciations)
            {
                query = query.Where(x => x.UsageRank != null && x.HasMultiplePronunciations == true);
            }
            else
                throw new NotSupportedException();

            if (noRecords)
                return new IndexEntry[0];

            return query;
        }
    }
}
