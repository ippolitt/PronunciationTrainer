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
    public class QuickRecorderTargetKey : IDatabaseTargetKey, IFileSystemTargetKey
    {
        private const string FileSystemRecordingFolder = "QuickRecorder";

        int IDatabaseTargetKey.TargetTypeId
        {
            get { return (int)AudioTargetType.QuickRecorder; }
        }

        string IDatabaseTargetKey.TargetKey
        {
            get { return Guid.Empty.ToString(); }
        }

        bool IFileSystemTargetKey.IsFolder
        {
            get { return true; }
        }

        string IFileSystemTargetKey.RelativeTargetPath
        {
            get { return FileSystemRecordingFolder; }
        }
    }
}
