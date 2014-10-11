using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pronunciation.Parser
{
    class FileLoaderMock : IFileLoader
    {
        private const string BaseFolder = @"D:\WORK\NET\PronunciationTrainer\Data\";
        private const string SourceFolderLDOCE = BaseFolder + @"LDOCE\Sounds";
        private const string DataFolderMW = BaseFolder + @"MW";

        public string GetBase64Content(string fileKey)
        {
            return "t";
        }

        public byte[] GetRawData(string fileKey)
        {
            //PrepareMissingFile(fileKey);
            //CheckFileMW(fileKey);
            return new byte[1];
        }

        public static void PrepareMissingFile(string fileKey)
        {
            if (fileKey.StartsWith(SoundManager.MW_SoundKeyPrefix))
            {
                PrepareFileMW(fileKey);
            }
            else if (fileKey.StartsWith(SoundManager.LDOCE_SoundKeyPrefix))
            {
                PrepareFileLDOCE(fileKey);
            }
        }

        private static void PrepareFileMW(string fileKey)
        {
            string fileName = string.Format("{0}.mp3", fileKey.Remove(0, SoundManager.MW_SoundKeyPrefix.Length));
            string sourceFile = Path.Combine(DataFolderMW, "Sounds", fileName);
            if (File.Exists(sourceFile))
                return;

            fileName = string.Format("{0}.wav", fileKey.Remove(0, SoundManager.MW_SoundKeyPrefix.Length));
            sourceFile = Path.Combine(DataFolderMW, "Extra", fileName);
            if (File.Exists(sourceFile))
            {
                CheckFolder(Path.Combine(DataFolderMW, "Active"));
                File.Move(sourceFile, Path.Combine(DataFolderMW, "Active", fileName));
            }
            else
            {
                throw new ArgumentException();
            }
        }

        private static void PrepareFileLDOCE(string fileKey)
        {
            string fileName = string.Format("{0}.mp3", fileKey.Remove(0, SoundManager.LDOCE_SoundKeyPrefix.Length));
            string sourceFile = Path.Combine(SourceFolderLDOCE, "SoundsUK", fileName);
            if (File.Exists(sourceFile))
                return;

            sourceFile = Path.Combine(SourceFolderLDOCE, "SoundsUS", fileName);
            if (File.Exists(sourceFile))
                return;

            fileName = string.Format("{0}.wav", fileKey.Remove(0, SoundManager.LDOCE_SoundKeyPrefix.Length));
            sourceFile = Path.Combine(SourceFolderLDOCE, "Extra", "SoundsUK", fileName);
            if (File.Exists(sourceFile))
            {
                CheckFolder(Path.Combine(SourceFolderLDOCE, "Active", "SoundsUK"));
                File.Move(sourceFile, Path.Combine(SourceFolderLDOCE, "Active", "SoundsUK", fileName));
            }
            else 
            {
                sourceFile = Path.Combine(SourceFolderLDOCE, "Extra", "SoundsUS", fileName);
                if (File.Exists(sourceFile))
                {
                    CheckFolder(Path.Combine(SourceFolderLDOCE, "Active", "SoundsUS"));
                    File.Move(sourceFile, Path.Combine(SourceFolderLDOCE, "Active", "SoundsUS", fileName));
                }
                else
                {
                    throw new ArgumentException();
                }   
            }
        }

        private static void CheckFolder(string targetFolder)
        {
            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }
        }

        public void Dispose()
        {
        }
    }
}
