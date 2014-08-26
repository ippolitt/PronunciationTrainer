namespace Pronunciation.Core.Database
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;

    public partial class Entities : DbContext
    {
        public Entities()
            : base("name=Entities")
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }

        public DbSet<Book> Books { get; set; }
        public DbSet<Exercise> Exercises { get; set; }
        public DbSet<ExerciseType> ExerciseTypes { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<RecordedAudio> RecordedAudios { get; set; }
        public DbSet<ExerciseAudio> ExerciseAudios { get; set; }
        public DbSet<Training> Trainings { get; set; }
        public DbSet<WordCategory> WordCategories { get; set; }
        public DbSet<WordCategoryMembership> WordCategoryMemberships { get; set; }
    }
}