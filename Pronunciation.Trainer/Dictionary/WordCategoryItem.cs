using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Trainer.Dictionary
{
    public class WordCategoryItem
    {
        public Guid CategoryId { get; private set; }
        public Guid MembershipId { get; private set; }

        public WordCategoryItem(Guid categoryId)
            : this(categoryId, Guid.NewGuid())
        {
        }

        public WordCategoryItem(Guid categoryId, Guid membershipId)
        {
            CategoryId = categoryId;
            MembershipId = membershipId;
        }
    }
}
