using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Utility;
using System.IO;

namespace Pronunciation.Core.Providers.Dictionary
{
    internal class DictionaryDATReader
    {
        private class DataIndex
        {
            public long Offset;
            public long Length;
        }

        private readonly DATFileReader _defaultAudioReader;
        private readonly DATFileReader _mwAudioReader;
        private readonly DATFileReader _htmlReader;
        private readonly object _syncLock = new object();

        private const string DefaultAudioDATFileName = "audio.dat";
        private const string MWAudioDATFileName = "audio_mw.dat";
        private const string HtmlDATFileName = "html.dat";
        private const int MWAudioFileId = 1;

        public DictionaryDATReader(string dataFolder)
        {
            _htmlReader = new DATFileReader(Path.Combine(dataFolder, HtmlDATFileName));
            _defaultAudioReader = new DATFileReader(Path.Combine(dataFolder, DefaultAudioDATFileName));
            _mwAudioReader = new DATFileReader(Path.Combine(dataFolder, MWAudioDATFileName));
        }

        public void WarmUp()
        {
            // Some systems may have an antivirus which would scan the file on first access.
            // On my computer with MS Security Essentials it took up to 3 seconds.
            lock (_syncLock)
            {
                _htmlReader.WarmUp();
            }
        }

        public byte[] GetHtmlData(string articleIndex)
        {
            var index = ParseIndex(articleIndex);
            lock (_syncLock)
            {
                return _htmlReader.GetData(index.Offset, index.Length);
            }
        }

        public byte[] GetAudioData(int? sourceFileId, string audioIndex)
        {
            DATFileReader audioReader;
            if (sourceFileId == null || sourceFileId.Value == 0)
            {
                audioReader = _defaultAudioReader;
            }
            else if (sourceFileId == MWAudioFileId)
            {
                audioReader = _mwAudioReader;
            }
            else
            {
                throw new ArgumentException(string.Format("Unknown value of .dat file ID: {0}", sourceFileId));
            }

            var index = ParseIndex(audioIndex);
            lock (_syncLock)
            {
                return audioReader.GetData(index.Offset, index.Length);
            }
        }

        private static DataIndex ParseIndex(string dataIndex)
        {
            string[] index = dataIndex.Split('|');
            if (index.Length != 2)
                throw new ArgumentException("Invalid format of data index!");

            return new DataIndex { Offset = long.Parse(index[0]), Length = long.Parse(index[1]) };
        }
    }
}
