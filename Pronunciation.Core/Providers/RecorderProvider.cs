using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pronunciation.Core.Providers
{
    public class RecorderProvider
    {
        private readonly string _recordingsFolder;

        public RecorderProvider(string recordingsFolder)
        {
            _recordingsFolder = recordingsFolder;
            if (!Directory.Exists(_recordingsFolder))
            {
                Directory.CreateDirectory(_recordingsFolder);
            }
        }

        public IEnumerable<KeyTextPair<string>> GetRecordingsList()
        {
            return Directory.GetFiles(_recordingsFolder, "*.mp3", SearchOption.TopDirectoryOnly)
                .Select(x => BuildRecordingInfo(x));
        }

        public string GetRecordingName(string recordingFilePath)
        {
            return Path.GetFileNameWithoutExtension(recordingFilePath);
        }

        public string BuildRecordingPath(string recordingName)
        {
            return Path.Combine(_recordingsFolder, string.Format("{0}.mp3", recordingName));
        }

        public string BuildNewRecordingName()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
        }

        public bool DeleteAllRecordings()
        {
            return DeleteFiles(Directory.GetFiles(_recordingsFolder, "*.mp3", SearchOption.TopDirectoryOnly));
        }

        public bool DeleteRecordings(IEnumerable<string> recordings)
        {
            return DeleteFiles(recordings.Select(x => BuildRecordingPath(x)));
        }

        private bool DeleteFiles(IEnumerable<string> filePaths)
        {
            bool hasErrors = false;
            foreach (var filePath in filePaths)
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (IOException)
                {
                    hasErrors = true;
                }
            }

            return !hasErrors;
        }

        private KeyTextPair<string> BuildRecordingInfo(string filePath)
        {
            string recordingName = GetRecordingName(filePath);
            return new KeyTextPair<string>(recordingName, recordingName);
        }
    }
}
