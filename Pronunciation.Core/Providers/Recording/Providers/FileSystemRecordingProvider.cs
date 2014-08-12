using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Contexts;
using System.IO;
using Pronunciation.Core.Providers.Recording.HistoryPolicies;

namespace Pronunciation.Core.Providers.Recording.Providers
{
    public class FileSystemRecordingProvider<T> : IRecordingProvider<T> where T : IFileSystemTargetKey
    {
        private readonly string _recordingsFolder;

        public FileSystemRecordingProvider(string recordingsFolder)
        {
            _recordingsFolder = recordingsFolder;
        }

        public bool ContainsAudios(T targetKey)
        {
            if (targetKey.IsFolder)
            {
                return GetFiles(targetKey.RelativeTargetPath).Length > 0;
            }
            else
            {
                return File.Exists(BuildFullFilePath(targetKey.RelativeTargetPath));
            }
        }

        public RecordedAudioListItem[] GetAudioList(T targetKey)
        {
            string[] files;
            if (targetKey.IsFolder)
            {
                files = GetFiles(targetKey.RelativeTargetPath);
            }
            else
            {
                string filePath = BuildFullFilePath(targetKey.RelativeTargetPath);
                files = File.Exists(filePath) ? new string[] { filePath } : new string[0];
            }

            return files.Select(x => new RecordedAudioListItem 
            {
                AudioKey = BuildAudioKey(x),
                RecordingDate = GetCreationDate(x)
            }).OrderByDescending(x => x.RecordingDate).ToArray();
        }

        public PlaybackData GetLatestAudio(T targetKey)
        {
            string filePath;
            if (targetKey.IsFolder)
            {
                filePath = GetFiles(targetKey.RelativeTargetPath).OrderByDescending(x => GetCreationDate(x))
                    .FirstOrDefault();
            }
            else
            {
                filePath = BuildFullFilePath(targetKey.RelativeTargetPath);
                if(!File.Exists(filePath))
                {
                    filePath = null;
                }
            }

            return string.IsNullOrEmpty(filePath) ? null : new PlaybackData(filePath);
        }

        public PlaybackData GetAudio(T targetKey, string audioKey)
        {
            string filePath;
            if (targetKey.IsFolder)
            {
                filePath = BuildFullFilePath(targetKey.RelativeTargetPath, audioKey);
            }
            else
            {
                filePath = BuildFullFilePath(targetKey.RelativeTargetPath);
            }

            return File.Exists(filePath) ? new PlaybackData(filePath) : null;
        }

        public RecordingSettings GetRecordingSettings(T targetKey)
        {
            string filePath;
            if (targetKey.IsFolder)
            {
                filePath = BuildFullFilePath(targetKey.RelativeTargetPath, BuildNewAudioFileName());
            }
            else
            {
                filePath = BuildFullFilePath(targetKey.RelativeTargetPath);
            }

            return new RecordingSettings(filePath);
        }

        public string RegisterNewAudio(T targetKey, DateTime recordingDate, string recordedFilePath,
            IRecordingHistoryPolicy recordingPolicy)
        {
            string newAudioKey = BuildAudioKey(recordedFilePath);
            if (targetKey.IsFolder)
            {
                // Delete the latest audio if the recording policy requires it to be overriden
                if (!(recordingPolicy is AlwaysAddRecordingPolicy))
                {
                    string latestAudioFilePath = GetFiles(targetKey.RelativeTargetPath)
                        .Where(x => !string.Equals(BuildAudioKey(x), newAudioKey, StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(x => GetCreationDate(x))
                        .FirstOrDefault();
                    if (!string.IsNullOrEmpty(latestAudioFilePath)
                        && recordingPolicy.OverrideLatestAudio(GetCreationDate(latestAudioFilePath)))
                    {
                        File.Delete(latestAudioFilePath);
                    }
                }
            }
            
            return newAudioKey;
        }

        public bool DeleteAudios(T targetKey, IEnumerable<string> audioKeys)
        {
            if (targetKey.IsFolder)
            {
                foreach (string audioKey in audioKeys)
                {
                    string filePath = BuildFullFilePath(targetKey.RelativeTargetPath, audioKey);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
            }
            else
            {
                string filePath = BuildFullFilePath(targetKey.RelativeTargetPath);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }

            return true;
        }

        public void DeleteTargetAudios(IEnumerable<T> targetKeys)
        {
            foreach (var targetKey in targetKeys)
            {
                if (targetKey.IsFolder)
                {
                    string folderPath = BuildFullFolderPath(targetKey.RelativeTargetPath);
                    if (Directory.Exists(folderPath))
                    {
                        Directory.Delete(folderPath);
                    }
                }
                else
                {
                    string filePath = BuildFullFilePath(targetKey.RelativeTargetPath);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
            }
        }

        public bool MoveAudios<K>(T fromKey, K toKey, IEnumerable<string> audioKeys) where K : IRecordingTargetKey
        {
            IFileSystemTargetKey destinationKey = (IFileSystemTargetKey)toKey;
            if (fromKey.IsFolder)
            {
                if (!destinationKey.IsFolder)
                    throw new InvalidOperationException("You can't move files from folder into a file!");

                foreach (string audioKey in audioKeys)
                {
                    string sourceFilePath = BuildFullFilePath(fromKey.RelativeTargetPath, audioKey);
                    string destinationFilePath = BuildFullFilePath(destinationKey.RelativeTargetPath, audioKey);
                    EnsureFolderExists(destinationFilePath);
                    File.Move(sourceFilePath, destinationFilePath);
                }
            }
            else
            {
                string sourceFilePath = BuildFullFilePath(fromKey.RelativeTargetPath);
                string destinationFilePath = destinationKey.IsFolder
                    ? BuildFullFilePath(destinationKey.RelativeTargetPath, BuildAudioKey(sourceFilePath))
                    : BuildFullFilePath(destinationKey.RelativeTargetPath);
                EnsureFolderExists(destinationFilePath);
                File.Move(sourceFilePath, destinationFilePath);
            }

            return true;
        }

        private string[] GetFiles(string relativeFolderPath)
        {
            string folderPath = BuildFullFolderPath(relativeFolderPath);
            return Directory.Exists(folderPath) 
                ? Directory.GetFiles(folderPath, "*.mp3", SearchOption.TopDirectoryOnly)
                : new string[0];
        }

        private string BuildFullFolderPath(string relativeFolderPath)
        {
            return Path.Combine(_recordingsFolder, relativeFolderPath);
        }

        private string BuildFullFilePath(string relativeFilePath)
        {
            return Path.Combine(_recordingsFolder, string.Format("{0}.mp3", relativeFilePath));
        }

        private string BuildFullFilePath(string relativeFolderPath, string fileName)
        {
            return Path.Combine(_recordingsFolder, relativeFolderPath, string.Format("{0}.mp3", fileName));
        }

        private void EnsureFolderExists(string filePath)
        {
            string folderPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
        }

        private string BuildAudioKey(string filePath)
        {
            return Path.GetFileNameWithoutExtension(filePath);
        }

        private string BuildNewAudioFileName()
        {
            return string.Format("{0:yyyy-MM-dd HH-mm-ss}", DateTime.Now);
        }

        private DateTime GetCreationDate(string filePath)
        {
            return File.GetCreationTime(filePath);
        }
    }
}
