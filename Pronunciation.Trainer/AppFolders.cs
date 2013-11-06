using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pronunciation.Trainer
{
    public class AppFolders
    {
        private readonly string _baseFolder;
        private readonly string _recordingsFolder;

        private const string _recordingsFolderName = "Recordings";
        private const string _recorderFolderName = "Recorder";
        private const string _dictionaryFolderName = "LPD";
        private const string _exercisesFolderName = "Exercises";

        public AppFolders(string baseFolder)
        {
            _baseFolder = baseFolder;
            _recordingsFolder = Path.Combine(baseFolder, _recordingsFolderName);
        }

        public string Base
        {
            get { return _baseFolder; }
        }

        public string Dictionary
        {
            get { return Path.Combine(_baseFolder, _dictionaryFolderName); }
        }

        public string DictionaryRecordings
        {
            get { return Path.Combine(_recordingsFolder, _dictionaryFolderName); }
        }

        public string Exercises
        {
            get { return Path.Combine(_baseFolder, _exercisesFolderName); }
        }

        public string ExercisesRecordings
        {
            get { return Path.Combine(_recordingsFolder, _exercisesFolderName); }
        }

        public string Recorder
        {
            get { return Path.Combine(_recordingsFolder, _recorderFolderName); }
        }
    }
}
