using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pronunciation.Parser
{
    class FileLoader : IFileLoader
    {
        private readonly DATIndexReader _readerLPD;
        private readonly DATIndexReader _readerLDOCE;
        private readonly DATIndexReader _readerMW;

        private const string SoundsDATFileName = "Sounds.dat";
        private const string SoundsIndexFileName = "Index.txt";

        public FileLoader(string sourceFolderLPD, string sourceFolderLDOCE, string sourceFolderMW)
        {
            _readerLPD = new DATIndexReader(
                Path.Combine(sourceFolderLPD, SoundsDATFileName),
                Path.Combine(sourceFolderLPD, SoundsIndexFileName));
            _readerLDOCE = new DATIndexReader(
                Path.Combine(sourceFolderLDOCE, SoundsDATFileName),
                Path.Combine(sourceFolderLDOCE, SoundsIndexFileName));
            _readerMW = new DATIndexReader(
                Path.Combine(sourceFolderMW, SoundsDATFileName),
                Path.Combine(sourceFolderMW, SoundsIndexFileName));
        }

        public string GetBase64Content(string fileKey)
        {
            byte[] rawData = GetRawData(fileKey);
            if (rawData == null)
                return null;

            return Convert.ToBase64String(rawData);
        }

        public byte[] GetRawData(string fileKey)
        {
            DATIndexReader reader;
            string originalKey;
            if (fileKey.StartsWith(SoundManager.MW_SoundKeyPrefix))
            {
                reader = _readerMW;
                originalKey = fileKey.Remove(0, SoundManager.MW_SoundKeyPrefix.Length);
            }
            else 
            {
                if (_readerLPD.ContainsKey(fileKey))
                {
                    reader = _readerLPD;
                    originalKey = fileKey;
                }
                else
                {
                    reader = _readerLDOCE;
                    originalKey = SoundManager.ConvertLPDToLDOCE(fileKey);
                }
            }

            return reader.GetData(originalKey);
        }

        public void Dispose()
        {
            _readerLPD.Dispose();
            _readerLDOCE.Dispose();
            _readerMW.Dispose();
        }
    }
}
