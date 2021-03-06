﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using Pronunciation.Trainer.Properties;
using System.IO;
using Pronunciation.Core.Providers;
using Pronunciation.Core.Providers.Recording;
using Pronunciation.Core.Providers.Training;
using Pronunciation.Core.Providers.Dictionary;
using Pronunciation.Core.Providers.Recording.Providers;
using Pronunciation.Core.Providers.Exercise;

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

    public enum RecordingHistoryMode
    {
        AlwaysOverrideLatest = 1,
        OverrideLatestAfterNDays = 2,
        AlwaysAdd = 3
    }

    public class AppSettings
    {
        public string BaseFolder { get; private set; }
        public int SampleRate { get; private set; }
        public int SkipRecordedAudioMs { get; private set; }
        public int MaxSamplesInWaveform { get; private set; }
        public int[] ActiveDictionaryIds { get; private set; }

        public StartupPlayMode StartupMode { get; set; }
        public RecordedPlayMode RecordedMode { get; set; }
        public RecordingHistoryMode HistoryMode { get; set; }
        public int HistoryDays { get; set; }
        public float ReferenceDataVolume { get; set; }
        public bool HighlightMultiPronunciationWords { get; set; }

        public AppFolders Folders { get; private set; }
        public AppFiles Files { get; private set; }
        public ConnectionStrings Connections { get; private set; }
        public RecordingProviders Recorders { get; private set; }

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
            HistoryMode = (RecordingHistoryMode)Settings.Default.RecordingHistoryMode;
            HistoryDays = Settings.Default.RecordingHistoryDays;
            MaxSamplesInWaveform = Settings.Default.MaxAudioSamplesInWaveform;
            HighlightMultiPronunciationWords = Settings.Default.HighlightMultiPronunciationWords;

            if (!string.IsNullOrEmpty(Settings.Default.ActiveDictionaryIds))
            {
                ActiveDictionaryIds = Settings.Default.ActiveDictionaryIds
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Select(x => int.Parse(x)).ToArray();
            }

            Folders = new AppFolders(Settings.Default.BaseFolder);
            Files = new AppFiles(Folders);
            Connections = new ConnectionStrings();
            Recorders = new RecordingProviders(Connections.Trainer, Folders.Recordings, Folders.Temp);
        }

        public void Save()
        {
            Settings.Default.StartupMode = (int)StartupMode;
            Settings.Default.RecordedMode = (int)RecordedMode;
            Settings.Default.ReferenceDataVolume = ReferenceDataVolume;
            Settings.Default.RecordingHistoryMode = (int)HistoryMode;
            Settings.Default.RecordingHistoryDays = HistoryDays;
            Settings.Default.HighlightMultiPronunciationWords = HighlightMultiPronunciationWords;
            Settings.Default.Save();
        }

        public class ConnectionStrings
        {
            private const string DBConnectionKey = "Trainer";

            public string Trainer { get; private set; }

            public ConnectionStrings()
            {
                Trainer = ConfigurationManager.ConnectionStrings[DBConnectionKey].ConnectionString;
            }
        }

        public class AppFolders
        {
            private readonly string _baseFolder;

            private const string AudioFolderName = "RecordedAudio";
            private const string RecordingsFolderName = "Recordings";
            private const string DictionaryFolderName = "Dictionary";
            private const string ExercisesFolderName = "Exercises";
            private const string DatabaseFolderName = "Database";
            private const string TheoryFolderName = "Theory";
            private const string TempFolderName = "Temp";
            private const string LogsFolderName = "Logs";

            public AppFolders(string baseFolder)
            {
                _baseFolder = baseFolder;
            }

            public string Base
            {
                get { return _baseFolder; }
            }

            public string Dictionary
            {
                get { return Path.Combine(_baseFolder, DictionaryFolderName); }
            }

            public string Exercises
            {
                get { return Path.Combine(_baseFolder, ExercisesFolderName); }
            }

            public string ExercisesRecordings
            {
                get { return Path.Combine(_baseFolder, AudioFolderName, ExercisesFolderName); }
            }

            public string Recordings
            {
                get { return Path.Combine(_baseFolder, RecordingsFolderName); }
            }

            public string Temp
            {
                get { return Path.Combine(_baseFolder, TempFolderName); }
            }

            public string Database
            {
                get { return Path.Combine(_baseFolder, DatabaseFolderName); }
            }

            public string Theory
            {
                get { return Path.Combine(_baseFolder, TheoryFolderName); }
            }

            public string Logs
            {
                get { return Path.Combine(_baseFolder, LogsFolderName); }
            }
        }

        public class AppFiles
        {
            private readonly AppFolders _folders;

            private const string LogFileName = "Trainer.log";

            public AppFiles(AppFolders folders)
            {
                _folders = folders;
            }

            public string Log
            {
                get { return Path.Combine(_folders.Logs, LogFileName); }
            }
        }

        public class RecordingProviders
        {
            public IRecordingProvider<TrainingTargetKey> Training { get; private set; }
            public IRecordingProvider<QuickRecorderTargetKey> QuickRecorder { get; private set; }
            public IRecordingProvider<DictionaryTargetKey> Dictionary { get; private set; }
            public IRecordingProvider<ExerciseTargetKey> Exercise { get; private set; }

            public RecordingProviders(string connectionString, string recordingsFolder, string tempFolder)
            {
                Training = new DatabaseRecordingProvider<TrainingTargetKey>(connectionString, tempFolder); 
                QuickRecorder = new DatabaseRecordingProvider<QuickRecorderTargetKey>(connectionString, tempFolder);
                Dictionary = new DatabaseRecordingProvider<DictionaryTargetKey>(connectionString, tempFolder);
                Exercise = new DatabaseRecordingProvider<ExerciseTargetKey>(connectionString, tempFolder);
            }
        }
    }
}
