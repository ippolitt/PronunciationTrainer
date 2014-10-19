using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    class WordRanks
    {
        public string LongmanSpoken;
        public string LongmanWritten;
        public int COCA;
        public int Longman;
        public bool IsAcademicWord;

        public int CalculateRank()
        {
            int rank;
            if (LongmanSpoken == "S1" || LongmanWritten == "W1" || (COCA > 0 && COCA <= 1000))
            {
                rank = 1000;
            }
            else if (LongmanSpoken == "S2" || LongmanWritten == "W2" || (COCA > 1000 && COCA <= 2000))
            {
                rank = 2000;
            }
            else if (LongmanSpoken == "S3" || LongmanWritten == "W3" || (COCA > 2000 && COCA <= 3000) || Longman == 3000)
            {
                rank = 3000;
            }
            else if ((COCA > 3000 && COCA <= 6000) || Longman == 6000)
            {
                rank = 6000;
            }
            else if ((COCA > 6000 && COCA <= 9000) || Longman == 9000)
            {
                rank = 9000;
            }
            else if (IsAcademicWord)
            {
                rank = 0;
            }
            else
            {
                throw new ArgumentException();
            }

            return rank;
        }
    }
}
