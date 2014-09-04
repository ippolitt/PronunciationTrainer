using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pronunciation.Parser
{
    class FileLoaderMock : IFileLoader
    {
        private const string SourceFolderLDOCE = @"D:\WORK\NET\PronunciationTrainer\Data\LDOCE\Sounds";

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
            //RegisterCheckedFile(fileKey);
            return "t";
        }

        public byte[] GetRawData(string fileKey)
        {
            //RegisterCheckedFile(fileKey);
            return new byte[1];
        }

        private void RegisterCheckedFile(string fileKey)
        {
            if (fileKey.StartsWith(LDOCEHtmlBuilder.AudioKeyPrefics))
            {
                string fileName = string.Format("{0}.wav", fileKey.Remove(0, LDOCEHtmlBuilder.AudioKeyPrefics.Length));
                string sourceFile = Path.Combine(SourceFolderLDOCE, "SoundsUK", fileName);
                if (File.Exists(sourceFile))
                {
                    //File.Move(sourceFile, Path.Combine(SourceFolderLDOCE, "Active", "SoundsUK", fileName));
                }
                else
                {
                    sourceFile = Path.Combine(SourceFolderLDOCE, "SoundsUS", fileName);
                    if (File.Exists(sourceFile))
                    {
                        //File.Move(sourceFile, Path.Combine(SourceFolderLDOCE, "Active", "SoundsUS", fileName));
                    }                    
                }

                //if (!File.Exists(sourceFile))
                //{
                //    Console.WriteLine("\r\nMissing sound file '{0}'", fileKey);
                //    //throw new ArgumentException();
                //}
            }
        }
    }
}
