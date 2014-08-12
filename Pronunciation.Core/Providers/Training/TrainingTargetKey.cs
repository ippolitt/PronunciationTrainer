using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Providers.Recording;
using Pronunciation.Core.Database;
using System.IO;
using Pronunciation.Core.Providers.Recording.Providers;

namespace Pronunciation.Core.Providers.Training
{
    public class TrainingTargetKey : IDatabaseTargetKey, IFileSystemTargetKey
    {
        public Guid TrainingId { get; private set; }

        private const string FileSystemRecordingFolder = "Training";

        public TrainingTargetKey(Guid trainingId)
        {
            TrainingId = trainingId;
        }

        int IDatabaseTargetKey.TargetTypeId
        {
            get { return (int)AudioTargetType.Training; }
        }

        string IDatabaseTargetKey.TargetKey
        {
            get { return TrainingId.ToString(); }
        }

        bool IFileSystemTargetKey.IsFolder
        {
            get { return true; }
        }

        string IFileSystemTargetKey.RelativeTargetPath
        {
            get { return Path.Combine(FileSystemRecordingFolder, TrainingId.ToString()); }
        }
    }
}
