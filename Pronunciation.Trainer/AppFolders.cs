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
        private const string _dictionaryRecordingsFolderName = "LPD";
        private const string _dictionaryFileFolderName = "LPD_File";
        private const string _dictionaryDBFolderName = "LPD_DB";
        private const string _exercisesFolderName = "Exercises";
        private const string _databaseFolderName = "Database";

        public AppFolders(string baseFolder)
        {
            _baseFolder = baseFolder;
            _recordingsFolder = Path.Combine(baseFolder, _recordingsFolderName);
        }

        public string Base
        {
            get { return _baseFolder; }
        }

        public string DictionaryFile
        {
            get { return Path.Combine(_baseFolder, _dictionaryFileFolderName); }
        }

        public string DictionaryDB
        {
            get { return Path.Combine(_baseFolder, _dictionaryDBFolderName); }
        }

        public string DictionaryRecordings
        {
            get { return Path.Combine(_recordingsFolder, _dictionaryRecordingsFolderName); }
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

        public string Database
        {
            get { return Path.Combine(_baseFolder, _databaseFolderName); }
        }
    }
}
