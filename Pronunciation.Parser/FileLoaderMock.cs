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

        public bool FlushCache()
        {
            return false;
        }

        public bool LoadCache(string cacheKey)
        {
            return false;
        }

        public void ClearCache()
        {
        }

        public string GetBase64Content(string fileKey)
        {
            return "t";
        }

        public byte[] GetRawData(string fileKey)
        {
            //CheckFileLDOCE(fileKey);
            //CheckFileMW(fileKey);
            return new byte[1];
        }

        private void CheckFileMW(string fileKey)
        {
            if (fileKey.StartsWith(SoundManager.MW_SoundKeyPrefix))
            {
                string fileName = string.Format("{0}.mp3", fileKey.Remove(0, SoundManager.MW_SoundKeyPrefix.Length));
                string activeFile = Path.Combine(DataFolderMW, "Sounds", fileName);
                if (File.Exists(activeFile))
                    return;

                fileName = string.Format("{0}.wav", fileKey.Remove(0, SoundManager.MW_SoundKeyPrefix.Length));
                string extraFile = Path.Combine(DataFolderMW, "Extra", fileName);
                if (File.Exists(extraFile))
                {
                    File.Move(extraFile, Path.Combine(DataFolderMW, "Active", fileName));
                }
                else
                {
                    throw new ArgumentException();
                }
            }
        }

        private void CheckFileLDOCE(string fileKey)
        {
            if (fileKey.StartsWith(SoundManager.LDOCE_SoundKeyPrefix))
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
                    File.Move(sourceFile, Path.Combine(SourceFolderLDOCE, "Active", "SoundsUK", fileName));
                }
                else 
                {
                    sourceFile = Path.Combine(SourceFolderLDOCE, "Extra", "SoundsUS", fileName);
                    if (File.Exists(sourceFile))
                    {
                        File.Move(sourceFile, Path.Combine(SourceFolderLDOCE, "Active", "SoundsUS", fileName));
                    }
                    else
                    {
                        throw new ArgumentException();
                    }   
                }
            }
        }
    }
}
