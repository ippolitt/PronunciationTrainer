using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Database;
using System.Collections.ObjectModel;
using System.Data.Entity;
using Pronunciation.Trainer.Views;
using System.Linq.Expressions;
using Pronunciation.Core;

namespace Pronunciation.Trainer.Database
{
    public class PronunciationDbContext
    {
        private readonly Entities _dbContext;
        private readonly Lazy<ObservableCollection<ExerciseListItem>> _exercises;
        private readonly Lazy<ObservableCollection<Topic>> _topics;
        private readonly Lazy<ObservableCollection<ExerciseType>> _exerciseTypes;
        private readonly Lazy<ObservableCollection<Book>> _books;
        private readonly Lazy<ObservableCollection<TrainingListItem>> _trainings;

        private readonly static Lazy<PronunciationDbContext> _instance = new Lazy<PronunciationDbContext>(
            () => new PronunciationDbContext());

        public static PronunciationDbContext Instance
        {
            get { return _instance.Value; }
        }

        private PronunciationDbContext()
        {
            _dbContext = new Entities();

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

            _exercises = new Lazy<ObservableCollection<ExerciseListItem>>(() =>
            {
                return new ObservableCollection<ExerciseListItem>(_dbContext.Exercises.AsNoTracking().Select(SelectExerciseExpression));
            });
            _trainings = new Lazy<ObservableCollection<TrainingListItem>>(() =>
            {
                return new ObservableCollection<TrainingListItem>(_dbContext.Trainings.AsNoTracking().Select(SelectTrainingExpression));
            });
        }

        public ObservableCollection<ExerciseListItem> Exercises
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

        public ObservableCollection<TrainingListItem> Trainings
        {
            get { return _trainings.Value; }
        }

        public Entities Target
        {
            get { return _dbContext; }
        }

        public void NotifyExerciseChanged(Guid exerciseId, bool isAdded)
        {
            var exercise = _dbContext.Exercises.AsNoTracking()
                .Where(x => x.ExerciseId == exerciseId)
                .Select(SelectExerciseExpression)
                .Single();
            if (isAdded)
            {
                Exercises.Add(exercise);
            }
            else
            {
                Exercises.Remove(Exercises.Single(x => x.ExerciseId == exerciseId));
                Exercises.Add(exercise);
            }
        }

        public void NotifyTrainingChanged(Guid trainingId, bool isAdded)
        {
            var training = _dbContext.Trainings.AsNoTracking()
                .Where(x => x.TrainingId == trainingId)
                .Select(SelectTrainingExpression)
                .Single();
            if (isAdded)
            {
                Trainings.Add(training);
            }
            else
            {
                Trainings.Remove(Trainings.Single(x => x.TrainingId == trainingId));
                Trainings.Add(training);
            }
        }

        public void RemoveExercises(ExerciseListItem[] exercises)
        {
            foreach (var exercise in exercises)
            {
                var fakeExercise = new Exercise { ExerciseId = exercise.ExerciseId };
                _dbContext.Exercises.Attach(fakeExercise);
                _dbContext.Exercises.Remove(fakeExercise);
            }
            _dbContext.SaveChanges();

            foreach (var exercise in exercises)
            {
                Exercises.Remove(exercise);
            }
        }

        public ExerciseAudioListItem[] GetExerciseAudios(Guid[] exerciseIds)
        {
            return _dbContext.ExerciseAudios.AsNoTracking()
                .Where(x => exerciseIds.Contains(x.ExerciseId))
                .Select(x => new ExerciseAudioListItem 
                    { 
                        AudioId = x.AudioId,
                        AudioName = x.AudioName, 
                        ExerciseId = x.ExerciseId
                    }).ToArray();
        }

        public void RemoveTrainings(TrainingListItem[] trainings)
        {
            foreach (var training in trainings)
            {
                var fakeTraining = new Training { TrainingId = training.TrainingId };
                _dbContext.Trainings.Attach(fakeTraining);
                _dbContext.Trainings.Remove(fakeTraining);
            }
            _dbContext.SaveChanges();

            foreach (var training in trainings)
            {
                Trainings.Remove(training);
            }
        }

        private Expression<Func<Training, TrainingListItem>> SelectTrainingExpression
        {
            get 
            {
                return x => new TrainingListItem
                {
                    TrainingId = x.TrainingId,
                    Title = x.Title,
                    Category = x.Category,
                    Created = x.Created,
                    CharacterCount = x.CharacterCount,
                    ReferenceAudioDuration = x.ReferenceAudioDuration
                };
            }
        }

        private Expression<Func<Exercise, ExerciseListItem>> SelectExerciseExpression
        {
            get 
            {
                return x => new ExerciseListItem
                {
                    ExerciseId = x.ExerciseId,
                    Title = x.Title,
                    TargetSound = x.TargetSound,
                    SourcePage = x.SourcePage,
                    SourceCD = x.SourceCD,
                    SourceTrack = x.SourceTrack,

                    ExerciseTypeId = x.ExerciseTypeId,
                    BookId = x.BookId,
                    TopicId = x.TopicId 
                };
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
