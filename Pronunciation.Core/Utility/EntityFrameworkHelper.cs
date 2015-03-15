using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pronunciation.Core.Database;

namespace Pronunciation.Core.Utility
{
    public class EntityFrameworkHelper
    {
        public static void WarmUpFramework()
        {
            using(var dbContext = new Entities())
            {
                dbContext.Books.AsNoTracking().Select(x => x.BookId).FirstOrDefault();
            }
        }
    }
}
