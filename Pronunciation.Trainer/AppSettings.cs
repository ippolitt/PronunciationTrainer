using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using Pronunciation.Trainer.Properties;
using System.IO;
using Pronunciation.Core.Providers;

namespace Pronunciation.Trainer
{
    public enum StartupPlayMode
    {
        None = 1,
        British = 2,
        American = 3
    }

    public enum RecordedPlayMode
    {
        RecordedOnly = 1,
        RecordedThenReference = 2,
        ReferenceThenRecorded = 3
    }

    public class AppSettings
    {
        public string BaseFolder { get; private set; }
        public int SampleRate { get; private set; }
        public int SkipRecordedAudioMs { get; private set; }

        public StartupPlayMode StartupMode { get; set; }
        public RecordedPlayMode RecordedMode { get; set; }
        public float ReferenceDataVolume { get; set; }

        public AppFolders Folders { get; private set; }
        public ConnectionStrings Connections { get; private set; }

        private readonly static Lazy<AppSettings> _instance = new Lazy<AppSettings>(() => new AppSettings());

        public static AppSettings Instance
        {
            get { return _instance.Value; }
        }

        private AppSettings()
        {
            BaseFolder = Settings.Default.BaseFolder;
            SampleRate = Settings.Default.SampleRate;
            StartupMode = (StartupPlayMode)Settings.Default.StartupMode;
            RecordedMode = (RecordedPlayMode)Settings.Default.RecordedMode;
            ReferenceDataVolume = Settings.Default.ReferenceDataVolume;
            SkipRecordedAudioMs = Settings.Default.SkipRecordedAudioMs;

            Folders = new AppFolders(Settings.Default.BaseFolder);
            Connections = new ConnectionStrings();
        }

        public void Save()
        {
            Settings.Default.StartupMode = (int)StartupMode;
            Settings.Default.RecordedMode = (int)RecordedMode;
            Settings.Default.ReferenceDataVolume = ReferenceDataVolume;
            Settings.Default.Save();
        }

        public class ConnectionStrings
        {
            private const string _lpdDatabaseKey = "LPD";

            public string LPD { get; private set; }

            public ConnectionStrings()
            {
                ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings[_lpdDatabaseKey];
                LPD = settings.ConnectionString;
            }
        }

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
}
