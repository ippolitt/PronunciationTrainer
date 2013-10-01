﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pronunciation.Core.Providers
{
    public class TrainingProvider
    {
        private readonly string _baseFolder;
        private readonly string _recordingsFolder;

        private const string _baseFolderName = "Exercises";
        private const string _recordingsFolderName = "Recordings";
        private const string _exerciseFileName = "main.png";

        private const string _bookFolderPrefix = "Book ";
        private const string _cdFolderPrefix = "CD";
        private const string _trackFolderPrefix = "";

        public TrainingProvider(string appFolder)
        {
            _baseFolder = Path.Combine(appFolder, _baseFolderName);
            _recordingsFolder = Path.Combine(_baseFolder, _recordingsFolderName);
        }

        public IEnumerable<KeyTextPair<string>> GetReferenceAudioList(ExerciseId exerciseId)
        {
            var audioFolder = BuildTrackFolderPath(exerciseId);
            if (!Directory.Exists(audioFolder))
                return null;

            return Directory.GetFiles(audioFolder, "*.mp3", SearchOption.TopDirectoryOnly)
                .Select(x => new KeyTextPair<string>(Path.GetFileNameWithoutExtension(x)))
                .OrderBy(x => new ComparableAudioName(x.Key));
        }

        public Uri GetExerciseImagePath(ExerciseId exerciseId)
        {
            var exerciseFile = Path.Combine(BuildTrackFolderPath(exerciseId), _exerciseFileName);
            if (!File.Exists(exerciseFile))
                return null;

            return new Uri(exerciseFile);
        }

        public string BuildReferenceAudioPath(ExerciseId exerciseId, string recordingName)
        {
            return Path.Combine(BuildTrackFolderPath(exerciseId), string.Format("{0}.mp3", recordingName));
        }

        public string BuildRecordedAudioPath(ExerciseId exerciseId, string recordingName)
        {
            return Path.Combine(BuildRecordedFolderPath(exerciseId), string.Format("{0}.mp3", recordingName));
        }

        private string BuildTrackFolderPath(ExerciseId exerciseId)
        {
            return Path.Combine(_baseFolder, BuildRelativeExercisePath(exerciseId));
        }

        private string BuildRecordedFolderPath(ExerciseId exerciseId)
        {
            return Path.Combine(_recordingsFolder, BuildRelativeExercisePath(exerciseId));
        }

        private string BuildRelativeExercisePath(ExerciseId exerciseId)
        {
            return string.Format(@"{0}{1}\{2}{3}\{4}{5}",
                _bookFolderPrefix, exerciseId.BookId,
                _cdFolderPrefix, exerciseId.CDNumber,
                _trackFolderPrefix, exerciseId.TrackNumber);
        }

        private class ComparableAudioName : IComparable<ComparableAudioName>
        {
            private class AudioNamePart : IComparable<AudioNamePart>
            {
                private int? _number;
                private string _text;

                public AudioNamePart(string namePart)
                {
                    int number;
                    if (int.TryParse(namePart, out number))
                    {
                        _number = number;
                    }
                    _text = namePart;
                }

                public int CompareTo(AudioNamePart other)
                {
                    if (_number.HasValue && other._number.HasValue)
                        return _number.Value.CompareTo(other._number.Value);

                    return _text.CompareTo(other._text);
                }
            }

            private readonly AudioNamePart _leftPart;
            private readonly AudioNamePart _rightPart;

            public ComparableAudioName(string audioName)
            {
                int splitIndex = 0;
                int rightPartIndex = 0;
                for (int i = 0; i < audioName.Length; i++)
                {
                    if (!Char.IsDigit(audioName, i))
                    {
                        splitIndex = i;
                        if (Char.IsLetterOrDigit(audioName, i))
                        {
                            rightPartIndex = i;
                        }
                        else if (i + 1 < audioName.Length)
                        {
                            rightPartIndex = i + 1;
                        }
                        break;
                    }
                }

                if (splitIndex > 0)
                {
                    _leftPart = new AudioNamePart(audioName.Substring(0, splitIndex));
                    if (rightPartIndex > 0)
                    {
                        _rightPart = new AudioNamePart(audioName.Substring(rightPartIndex));
                    }
                }
                else
                {
                    _leftPart = new AudioNamePart(audioName);
                }
            }

            public int CompareTo(ComparableAudioName other)
            {
                int result = _leftPart.CompareTo(other._leftPart);
                if (result == 0)
                {
                    if (_rightPart != null && other._rightPart != null)
                    {
                        result = _rightPart.CompareTo(other._rightPart); 
                    }
                    else if (_rightPart != null)
                    {
                        result = 1;
                    }
                    else if (other._rightPart != null)
                    {
                        result = -1;
                    }
                    else
                    {
                        result = 0;
                    }
                }

                return result;
            }
        }
    }
}
