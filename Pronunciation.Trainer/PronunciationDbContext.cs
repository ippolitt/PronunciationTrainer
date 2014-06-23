using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Database;
using System.Collections.ObjectModel;
using System.Data.Entity;
using Pronunciation.Trainer.Views;

namespace Pronunciation.Trainer
{
    public class PronunciationDbContext
    {
        private readonly Entities _dbContext;
        private readonly Lazy<ObservableCollection<Exercise>> _exercises;
        private readonly Lazy<ObservableCollection<Topic>> _topics;
        private readonly Lazy<ObservableCollection<ExerciseType>> _exerciseTypes;
        private readonly Lazy<ObservableCollection<Book>> _books;
        private readonly Lazy<ObservableCollection<RecordingLight>> _recordings;

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

            _exercises = new Lazy<ObservableCollection<Exercise>>(() =>
            {
                return new ObservableCollection<Exercise>(_dbContext.Exercises.AsNoTracking());
            });
            _recordings = new Lazy<ObservableCollection<RecordingLight>>(() =>
            {
                return new ObservableCollection<RecordingLight>(_dbContext.Recordings.AsNoTracking().Select(FillRecordingLight));
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

        public ObservableCollection<RecordingLight> Recordings
        {
            get { return _recordings.Value; }
        }

        public Entities Target
        {
            get { return _dbContext; }
        }

        public void NotifyExerciseChanged(Guid exerciseId, bool isAdded)
        {
            var exercise = _dbContext.Exercises.AsNoTracking().Single(x => x.ExerciseId == exerciseId);
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

        public void NotifyRecordingChanged(Guid recordingId, bool isAdded)
        {
            var recording = _dbContext.Recordings.AsNoTracking()
                .Where(x => x.RecordingId == recordingId)
                .Select(FillRecordingLight).Single();
            if (isAdded)
            {
                Recordings.Add(recording);
            }
            else
            {
                Recordings.Remove(Recordings.Single(x => x.RecordingId == recordingId));
                Recordings.Add(recording);
            }
        }

        public void RemoveExercises(Exercise[] exercises)
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

        public void RemoveRecordings(RecordingLight[] recordings)
        {
            foreach (var recording in recordings)
            {
                var fakeRecording = new Recording { RecordingId = recording.RecordingId };
                _dbContext.Recordings.Attach(fakeRecording);
                _dbContext.Recordings.Remove(fakeRecording);
            }
            _dbContext.SaveChanges();

            foreach (var recording in recordings)
            {
                Recordings.Remove(recording);
            }
        }

        private RecordingLight FillRecordingLight(Recording x)
        {
            return new RecordingLight
            {
                RecordingId = x.RecordingId,
                Title = x.Title,
                Category = x.Category,
                Created = x.Created
            };
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
