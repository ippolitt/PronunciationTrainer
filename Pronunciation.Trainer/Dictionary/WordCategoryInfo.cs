using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Trainer.Dictionary
{
    public class WordCategoryInfo
    {
        public bool IsInFavorites { get; private set; }
        public Guid[] Categories { get; private set; }

        public WordCategoryInfo(bool isInFavorites, Guid[] categories)
        {
            IsInFavorites = isInFavorites;
            Categories = categories;
        }
    }
}
