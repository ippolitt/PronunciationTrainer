using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Trainer.Dictionary
{
    public class WordMembershipItem
    {
        public Guid CategoryId { get; private set; }
        public Guid MembershipId { get; private set; }

        public WordMembershipItem(Guid categoryId)
            : this(categoryId, Guid.NewGuid())
        {
        }

        public WordMembershipItem(Guid categoryId, Guid membershipId)
        {
            CategoryId = categoryId;
            MembershipId = membershipId;
        }
    }
}
