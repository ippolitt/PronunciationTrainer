using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ookii.Dialogs.Wpf;
using System.IO;
using Pronunciation.Core.Providers.Recording;
using Pronunciation.Core.Contexts;
using System.Windows;
using Pronunciation.Trainer.Views;
using Pronunciation.Core.Providers.Exercise;
using Pronunciation.Trainer.Utility;

namespace Pronunciation.Trainer.Export
{
    public class AudioExporter
    {
        private readonly Window _dialogOwner;
        private const string ExerciseRecordingsFolderName = "Recordings";

        private class AudioExportInfo
        {
            public Func<PlaybackData> AudioDataProvider;
            public string DestinationFile;
            public bool IsFileExists;
            public bool IsReferenceAudio;
            public bool WasExported;
        }

        public AudioExporter(Window dialogOwner)
        {
            _dialogOwner = dialogOwner;
        }

        public void ExportRecordings(RecordingProviderWithTargetKey provider, IEnumerable<RecordedAudioListItem> recordings)
        {
            string targetFolder = GetExportFolderPath("Select a folder to export the selected recordings to");
            if (string.IsNullOrEmpty(targetFolder))
                return;

            bool checkExistance = true;
            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
                checkExistance = false;
            }

            List<AudioExportInfo> exportItems = BuildExportInfo(provider, recordings, targetFolder, checkExistance);
            bool fullSuccess = ExportAudios(exportItems);
            string message;
            if (fullSuccess)
            {
                message = "All the selected recordings have been exported succesfully.";
            }
            else
            {
                int exportedCount = exportItems.Count(x => x.WasExported);
                if (exportedCount == 0)
                {
                    message = "No recordings have been exported.";
                }
                else
                {
                    message = string.Format("Exported {0} of {1} selected recordings.", exportedCount, exportItems.Count);
                }
            }
            MessageHelper.ShowInfo(message, "Export result");
        }

        public void ExportExerciseAudios(IRecordingProvider<ExerciseTargetKey> provider, 
            IEnumerable<ExerciseAudioListItemWithData> audios)
        {
            string targetFolder = GetExportFolderPath("Select a folder to export the selected audios to");
            if (string.IsNullOrEmpty(targetFolder))
                return;

            bool checkExistance = true;
            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
                checkExistance = false;
            }

            List<AudioExportInfo> exportItems = BuildExportInfo(provider, audios, targetFolder, checkExistance);
            bool fullSuccess = ExportAudios(exportItems);
            string message;
            if (fullSuccess)
            {
                if (exportItems.Count(x => !x.IsReferenceAudio) > 0)
                {
                    message = "All the selected audios along with the assosiated recordings have been exported succesfully.";
                }
                else
                {
                    message = "All the selected audios have been exported succesfully.";
                }
            }
            else
            {
                int exportedReferenceCount = exportItems.Count(x => x.IsReferenceAudio && x.WasExported);
                int exportedRecordingCount = exportItems.Count(x => !x.IsReferenceAudio && x.WasExported);
                if (exportedReferenceCount == 0 && exportedRecordingCount == 0)
                {
                    message = "No audios have been exported.";
                }
                else
                {
                    message = string.Format(
                        "Exported {0} of {1} selected audios and {2} of {3} assosiated recordings.",
                        exportedReferenceCount, exportItems.Count(x => x.IsReferenceAudio),
                        exportedRecordingCount, exportItems.Count(x => !x.IsReferenceAudio));
                }
            }
            MessageHelper.ShowInfo(message, "Export result");
        }

        private string BuildRecordingFileName(RecordedAudioListItem recording)
        {
            return string.Format("{0:yyyy-MM-dd HH-mm-ss}.mp3", recording.RecordingDate);
        }

        private string BuildExerciseAudioFileName(ExerciseAudioListItem audio)
        {
            return string.Format("{0}.mp3", audio.AudioName);
        }

        private string GetExportFolderPath(string dialogTitle)
        {
            var dlg = new VistaFolderBrowserDialog();
            dlg.ShowNewFolderButton = true;
            dlg.UseDescriptionForTitle = true;
            dlg.Description = dialogTitle;

            bool? result = dlg.ShowDialog();
            return result == true ? dlg.SelectedPath : null;
        }

        private List<AudioExportInfo> BuildExportInfo(RecordingProviderWithTargetKey provider,
            IEnumerable<RecordedAudioListItem> recordings, string targetFolder, bool checkExistance)
        {
            var exportInfos = new List<AudioExportInfo>();
            foreach (var recording in recordings)
            {
                string audioKey = recording.AudioKey;
                string destinationFile = Path.Combine(targetFolder, BuildRecordingFileName(recording));
                exportInfos.Add(new AudioExportInfo
                {
                    AudioDataProvider = () => provider.GetAudio(audioKey),
                    DestinationFile = destinationFile,
                    IsReferenceAudio = false,
                    IsFileExists = checkExistance && File.Exists(destinationFile)
                });
            }

            return exportInfos;
        }

        private List<AudioExportInfo> BuildExportInfo(IRecordingProvider<ExerciseTargetKey> provider,
            IEnumerable<ExerciseAudioListItemWithData> audios, string targetFolder, bool checkExistance)
        {
            var exportInfos = new List<AudioExportInfo>();
            string recordingsFolder = null;
            foreach (var audio in audios)
            {
                byte[] referenceData = audio.RawData;
                string fileName = BuildExerciseAudioFileName(audio);
                string audioDestinationFile = Path.Combine(targetFolder, fileName);
                exportInfos.Add(new AudioExportInfo
                {
                    AudioDataProvider = () => new PlaybackData(referenceData),
                    DestinationFile = audioDestinationFile,
                    IsReferenceAudio = true,
                    IsFileExists = checkExistance && File.Exists(audioDestinationFile)
                });

                PlaybackData recordedAudio = provider.GetLatestAudio(new ExerciseTargetKey(audio.ExerciseId, audio.AudioName));
                if (recordedAudio != null)
                {
                    if (recordingsFolder == null)
                    {
                        recordingsFolder = Path.Combine(targetFolder, ExerciseRecordingsFolderName);
                        if (!Directory.Exists(recordingsFolder))
                        {
                            Directory.CreateDirectory(recordingsFolder);
                        }
                    }

                    string recordingDestinationFile = Path.Combine(recordingsFolder, fileName);
                    exportInfos.Add(new AudioExportInfo
                    {
                        AudioDataProvider = () => recordedAudio,
                        DestinationFile = recordingDestinationFile,
                        IsReferenceAudio = false,
                        IsFileExists = checkExistance && File.Exists(recordingDestinationFile)
                    });
                }
            }

            return exportInfos;
        }

        private bool ExportAudios(List<AudioExportInfo> exportItems)
        {
            int conflictsCount = exportItems.Count(x => x.IsFileExists);
            int exportedFilesCount = 0;
            FileOverrideDialog.FileOverrideAction? nextAction = null;
            foreach (var exportItem in exportItems)
            {
                if (exportItem.IsFileExists)
                {
                    conflictsCount--;
                    FileOverrideDialog.FileOverrideAction currentAction;
                    if (nextAction == null)
                    {
                        var dialog = new FileOverrideDialog();
                        dialog.Owner = _dialogOwner;
                        dialog.InitArguments(exportItem.DestinationFile, conflictsCount);
                        if (dialog.ShowDialog() == true && dialog.OverrideResult != null)
                        {
                            currentAction = dialog.OverrideResult.OverrideAction;
                            if (dialog.OverrideResult.ApplyToNextConflicts)
                            {
                                nextAction = currentAction;
                            }
                        }
                        else
                        {
                            // Default action if the dialog has been closed with the X
                            currentAction = FileOverrideDialog.FileOverrideAction.Abort;
                        }
                    }
                    else
                    {
                        currentAction = nextAction.Value;
                    }

                    if (currentAction == FileOverrideDialog.FileOverrideAction.Skip)
                    {
                        continue;
                    }
                    else if (currentAction == FileOverrideDialog.FileOverrideAction.Abort)
                    {
                        break;
                    }
                }

                PlaybackData audioData = exportItem.AudioDataProvider();
                try
                {
                    if (audioData.IsFilePath)
                    {
                        File.Copy(audioData.FilePath, exportItem.DestinationFile, true);
                    }
                    else
                    {
                        File.WriteAllBytes(exportItem.DestinationFile, audioData.RawData);
                    }
                    exportItem.WasExported = true;
                    exportedFilesCount++;
                }
                catch (Exception ex)
                {
                    MessageHelper.ShowError(string.Format(
                        "Couldn't export audio to file '{0}' because of the following error: {1}",
                        Path.GetFileName(exportItem.DestinationFile), ex.Message));
                }
            }

            return (exportedFilesCount > 0 && exportedFilesCount == exportItems.Count);
        }
    }
}
