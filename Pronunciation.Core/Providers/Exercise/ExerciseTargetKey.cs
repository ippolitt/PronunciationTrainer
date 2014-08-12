using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Providers.Recording.Providers;
using Pronunciation.Core.Database;
using System.IO;

namespace Pronunciation.Core.Providers.Exercise
{
    public class ExerciseTargetKey : IDatabaseTargetKey, IFileSystemTargetKey
    {
        public Guid ExerciseId { get; private set; }
        public string AudioName { get; private set; }

        private const string FileSystemRecordingFolder = "Exercise";

        public ExerciseTargetKey(Guid exerciseId, string audioName)
        {
            ExerciseId = exerciseId;
            AudioName = audioName;
        }

        int IDatabaseTargetKey.TargetTypeId
        {
            get { return (int)AudioTargetType.Exercise; }
        }

        string IDatabaseTargetKey.TargetKey
        {
            get { return string.Format("{0}|{1}", ExerciseId, AudioName); }
        }

        bool IFileSystemTargetKey.IsFolder
        {
            get { return false; }
        }

        string IFileSystemTargetKey.RelativeTargetPath
        {
            get { return Path.Combine(FileSystemRecordingFolder, ExerciseId.ToString(), AudioName); }
        }
    }
}
