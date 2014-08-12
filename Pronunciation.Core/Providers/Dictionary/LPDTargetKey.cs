using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Providers.Recording;
using Pronunciation.Core.Database;
using System.IO;
using Pronunciation.Core.Providers.Recording.Providers;

namespace Pronunciation.Core.Providers.Dictionary
{
    public class LPDTargetKey : IDatabaseTargetKey, IFileSystemTargetKey
    {
        public string SoundKey { get; private set; }

        private const string DatabaseKeyPrefix = "lpd|";
        private const string FileSystemRecordingFolder = "LPD";

        public LPDTargetKey(string soundKey)
        {
            SoundKey = soundKey;
        }

        int IDatabaseTargetKey.TargetTypeId
        {
            get { return (int)AudioTargetType.LPD; }
        }

        string IDatabaseTargetKey.TargetKey
        {
            get { return DatabaseKeyPrefix + SoundKey; }
        }

        bool IFileSystemTargetKey.IsFolder
        {
            get { return false; }
        }

        string IFileSystemTargetKey.RelativeTargetPath
        {
            get { return Path.Combine(FileSystemRecordingFolder, SoundKey); }
        }
    }
}
