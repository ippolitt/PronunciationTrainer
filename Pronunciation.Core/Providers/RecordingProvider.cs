using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pronunciation.Core.Providers
{
    public class RecordingProvider
    {
        private readonly string _recordingsFolder;

        public RecordingProvider(string recordingsFolder)
        {
            _recordingsFolder = recordingsFolder;
            if (!Directory.Exists(_recordingsFolder))
            {
                Directory.CreateDirectory(_recordingsFolder);
            }
        }

        public string BuildNewAudioName()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
        }

        public string GetAudioName(string audioFilePath)
        {
            return Path.GetFileNameWithoutExtension(audioFilePath);
        }

        public IEnumerable<KeyTextPair<string>> GetAudioList(Guid recordingId)
        {
            string audioFolder = BuildAudioFolderPath(recordingId);
            if (!Directory.Exists(audioFolder))
                return null;

            return Directory.GetFiles(audioFolder, "*.mp3", SearchOption.TopDirectoryOnly)
                .Select(x => BuildAudioInfo(x));
        }

        public string BuildAudioFolderPath(Guid recordingId)
        {
            return Path.Combine(_recordingsFolder, recordingId.ToString());
        }

        public string BuildAudioFilePath(Guid recordingId, string audioName)
        {
            return Path.Combine(BuildAudioFolderPath(recordingId), string.Format("{0}.mp3", audioName));
        }

        public bool DeleteAudio(Guid recordingId, IEnumerable<string> audioNames)
        {
            return DeleteFiles(audioNames.Select(x => BuildAudioFilePath(recordingId, x)));
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

        private KeyTextPair<string> BuildAudioInfo(string filePath)
        {
            string audioName = GetAudioName(filePath);
            return new KeyTextPair<string>(audioName, audioName);
        }
    }
}
