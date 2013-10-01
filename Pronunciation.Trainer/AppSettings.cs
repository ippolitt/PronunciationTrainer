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

        public StartupPlayMode StartupMode { get; set; }
        public RecordedPlayMode RecordedMode { get; set; }
        public float RecordingInterval { get; set; }
        public float ReferenceDataVolume { get; set; }

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
            RecordingInterval = Settings.Default.RecordingInterval;
            ReferenceDataVolume = Settings.Default.ReferenceDataVolume;
        }

        public void Save()
        {
            Settings.Default.StartupMode = (int)StartupMode;
            Settings.Default.RecordedMode = (int)RecordedMode;
            Settings.Default.RecordingInterval = RecordingInterval;
            Settings.Default.ReferenceDataVolume = ReferenceDataVolume;
            Settings.Default.Save();
        }
    }
}
