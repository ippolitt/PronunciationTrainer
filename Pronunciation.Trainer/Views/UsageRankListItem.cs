using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Pronunciation.Trainer.Views
{
    public class UsageRankListItem
    {
        public int Rank { get; private set; }
        public string DisplayName { get; private set; }
        public bool IsServiceItem { get; private set; }

        public UsageRankListItem(string displayName)
        {
            DisplayName = displayName;
            IsServiceItem = true;
        }

        public UsageRankListItem(int rank, string displayName)
        {
            Rank = rank;
            DisplayName = displayName;
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
