using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Database;
using System.Collections.ObjectModel;
using System.Data.Entity;

namespace Pronunciation.Trainer
{
    public class PronunciationDbContext
    {
        private readonly Entities _dbContext;
        private readonly Lazy<ObservableCollection<Exercise>> _exercises;
        private readonly Lazy<ObservableCollection<Topic>> _topics;
        private readonly Lazy<ObservableCollection<ExerciseType>> _exerciseTypes;
        private readonly Lazy<ObservableCollection<Book>> _books;

        public delegate void EntryChangedHandler(int entryId, bool isAdded);
        public event EntryChangedHandler ExerciseChanged;

        public PronunciationDbContext()
        {
            _dbContext = new Entities();

            _exercises = new Lazy<ObservableCollection<Exercise>>(() => 
            { 
                _dbContext.Exercises.Load();
                return _dbContext.Exercises.Local;
            });
            _topics = new Lazy<ObservableCollection<Topic>>(() =>
            {
                _dbContext.Topics.Load();
                return _dbContext.Topics.Local;
            });
            _exerciseTypes = new Lazy<ObservableCollection<ExerciseType>>(() =>
            {
                _dbContext.ExerciseTypes.Load();
                return _dbContext.ExerciseTypes.Local;
            });
            _books = new Lazy<ObservableCollection<Book>>(() =>
            {
                _dbContext.Books.Load();
                return _dbContext.Books.Local;
            });
        }

        public ObservableCollection<Exercise> Exercises
        {
            get { return _exercises.Value; }
        }

        public ObservableCollection<Topic> Topics
        {
            get { return _topics.Value; }
        }

        public ObservableCollection<ExerciseType> ExerciseTypes
        {
            get { return _exerciseTypes.Value; }
        }

        public ObservableCollection<Book> Books
        {
            get { return _books.Value; }
        }

        public Entities Target
        {
            get { return _dbContext; }
        }

        public void NotifyExerciseChanged(int exerciseId, bool isAdded)
        {
            if (isAdded)
            {
                _dbContext.Exercises.Load();
            }
            else
            {
                var exercise = _dbContext.Exercises.Single(x => x.ExerciseId == exerciseId);
                _dbContext.Entry(exercise).Reload();
            }

            if (ExerciseChanged != null)
            {
                ExerciseChanged(exerciseId, isAdded);
            }
        }

        //((System.Data.Entity.Infrastructure.IObjectContextAdapter)_dataContext).ObjectContext

        // For updated:
        //context.Entry(myEntity).CurrentValues.SetValues(context.Entry(myEntity).OriginalValues);
        // or
        //context.Entry(myEntity).State = EntityState.Unchanged; 
        // For added:
        //context.Entry(myEntity).State = EntityState.Detached; 
        // For deleted:
        //context.Entry(myEntity).Reload(); 
    }
}
