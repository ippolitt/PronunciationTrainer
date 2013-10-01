using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using System.Data;

namespace Pronunciation.Core.Database
{
    public static class DbExtensions
    {
        public static bool HasChanges(this DbContext dbContext)
        {
            return dbContext.ChangeTracker.Entries().Any(e =>
                e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted);
        }
    }
}
