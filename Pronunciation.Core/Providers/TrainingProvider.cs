using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pronunciation.Core.Providers
{
    public class TrainingProvider
    {
        private readonly string _sourceFolder;
        private readonly string _recordingsFolder;

        private const string _exerciseFileName = "main.png";
        private const string _cdFolderPrefix = "CD";
        private const string _trackFolderPrefix = "";

        public TrainingProvider(string sourceFolder, string recordingsFolder)
        {
            _sourceFolder = sourceFolder;
            _recordingsFolder = recordingsFolder;
        }

        public IEnumerable<KeyTextPair<string>> GetReferenceAudioList(ExerciseKey exerciseId)
        {
            var audioFolder = BuildTrackFolderPath(exerciseId);
            if (!Directory.Exists(audioFolder))
                return null;

            return Directory.GetFiles(audioFolder, "*.mp3", SearchOption.TopDirectoryOnly)
                .Select(x => new KeyTextPair<string>(Path.GetFileNameWithoutExtension(x)))
                .OrderBy(x => new MultipartName(x.Key));
        }

        public Uri GetExerciseImagePath(ExerciseKey exerciseId)
        {
            var exerciseFile = Path.Combine(BuildTrackFolderPath(exerciseId), _exerciseFileName);
            if (!File.Exists(exerciseFile))
                return null;

            return new Uri(exerciseFile);
        }

        public string BuildReferenceAudioPath(ExerciseKey exerciseId, string recordingName)
        {
            return Path.Combine(BuildTrackFolderPath(exerciseId), string.Format("{0}.mp3", recordingName));
        }

        public string BuildRecordedAudioPath(ExerciseKey exerciseId, string recordingName)
        {
            return Path.Combine(BuildRecordedFolderPath(exerciseId), string.Format("{0}.mp3", recordingName));
        }

        private string BuildTrackFolderPath(ExerciseKey exerciseId)
        {
            return Path.Combine(_sourceFolder, BuildRelativeExercisePath(exerciseId));
        }

        private string BuildRecordedFolderPath(ExerciseKey exerciseId)
        {
            return Path.Combine(_recordingsFolder, BuildRelativeExercisePath(exerciseId));
        }

        private string BuildRelativeExercisePath(ExerciseKey exerciseId)
        {
            return string.Format(@"{0}\{1}{2}\{3}{4}",
                exerciseId.BookKey,
                _cdFolderPrefix, exerciseId.CDNumber,
                _trackFolderPrefix, exerciseId.TrackNumber);
        }
    }
}
