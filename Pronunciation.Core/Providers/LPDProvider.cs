using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Pronunciation.Core.Contexts;

namespace Pronunciation.Core.Providers
{
    public abstract class LPDProvider
    {
        protected string BaseFolder { get; private set; }
        protected string RecordingsFolder { get; private set; }

        public LPDProvider(string baseFolder, string recordingsFolder)
        {
            BaseFolder = baseFolder;
            RecordingsFolder = recordingsFolder;
        }

        public List<KeyTextPair<string>> GetWordLists()
        {
            return new List<KeyTextPair<string>> { 
                new KeyTextPair<string>("1000", "Top 1000 words"),
                new KeyTextPair<string>("2000", "Top 2000 words"),
                new KeyTextPair<string>("3000", "Top 3000 words"),
                new KeyTextPair<string>("5000", "Top 5000 words"),
                new KeyTextPair<string>("7500", "Top 7500 words")
            };
        }

        public PageInfo LoadListPage(string pageKey)
        {
            return new PageInfo(false, pageKey, BuildWordListPath(pageKey));
        }

        public PlaybackSettings GetRecordedAudio(string audioKey)
        {
            string recordedFilePath = BuildRecordingFilePath(audioKey);
            if (!File.Exists(recordedFilePath))
                return null;

            return new PlaybackSettings(recordedFilePath);
        }

        public bool IsRecordedAudioExists(string audioKey)
        {
            return File.Exists(BuildRecordingFilePath(audioKey));
        }

        public RecordingSettings GetRecordingSettings(string audioKey)
        {
            return new RecordingSettings(BuildRecordingFilePath(audioKey));
        }

        private string BuildRecordingFilePath(string audioKey)
        {
            return Path.Combine(RecordingsFolder, string.Format(@"{0}.mp3", audioKey));
        }

        protected Uri BuildWordListPath(string listName)
        {
            return new Uri(Path.Combine(BaseFolder, string.Format(@"{0}.html", listName.ToLower())));
        }
    }
}
