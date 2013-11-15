using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using Pronunciation.Trainer.Properties;
using System.IO;

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
    }
}
