using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Trainer.Dictionary
{
    public class WordCategoryInfo
    {
        public bool IsInFavorites { get; set; }
        public List<Guid> Categories { get; set; }
    }
}
