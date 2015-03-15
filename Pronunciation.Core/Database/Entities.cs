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

        public virtual DbSet<Book> Books { get; set; }
        public virtual DbSet<Exercise> Exercises { get; set; }
        public virtual DbSet<ExerciseType> ExerciseTypes { get; set; }
        public virtual DbSet<Topic> Topics { get; set; }
        public virtual DbSet<RecordedAudio> RecordedAudios { get; set; }
        public virtual DbSet<ExerciseAudio> ExerciseAudios { get; set; }
        public virtual DbSet<Training> Trainings { get; set; }
        public virtual DbSet<DictionaryCategory> DictionaryCategories { get; set; }
        public virtual DbSet<DictionaryWord> DictionaryWords { get; set; }
    }
}