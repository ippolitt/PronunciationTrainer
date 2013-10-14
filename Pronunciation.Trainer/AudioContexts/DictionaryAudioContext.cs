using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Contexts;
using Pronunciation.Core.Providers;
using System.IO;
using System.Windows.Controls;
using System.Runtime.InteropServices;

namespace Pronunciation.Trainer.AudioContexts
{
    [ComVisibleAttribute(true)]
    public class DictionaryAudioContext : IAudioContext
    {
        private readonly LPDProvider _provider;
        private readonly GetPageAudioHandler _audioDataLoader;
        private PageInfo _currentPage;
        private string _referenceAudioData;
        private bool _useUkAudio;

        private const string GetPageAudioMethodName = "extGetAudioData";
        private const int FirstUkAudioCode = 1;
        private const int FirstUsAudioCode = 2;

        public event AudioContextChangedHandler ContextChanged;
        public delegate string GetPageAudioHandler(string methodName, object[] methodArgs);

        public DictionaryAudioContext(LPDProvider provider, GetPageAudioHandler audioDataLoader)
        {
            _provider = provider;
            _audioDataLoader = audioDataLoader;
        }

        public void RefreshContext(Uri pageUrl, bool useUkAudio, bool playImmediately)
        {
            _referenceAudioData = null;
            _useUkAudio = useUkAudio;
            _currentPage = _provider.GetPageInfo(pageUrl);

            if (ContextChanged != null)
            {
                ContextChanged(playImmediately ? PlayAudioMode.PlayReference : PlayAudioMode.None);
            }
        }

        // This method to be called from JScript
        public void PlayAudioExt(string audioData)
        {
            _referenceAudioData = audioData;
            if (!string.IsNullOrEmpty(audioData) && ContextChanged != null)
            {
                ContextChanged(PlayAudioMode.PlayReference);
            }
        }

        public PageInfo ActivePage
        {
            get { return _currentPage; }
        }

        public bool IsReferenceAudioExists
        {
            get { return IsWord; }
        }

        public bool IsRecordedAudioExists
        {
            get
            {
                if (!IsWord)
                    return false;

                return File.Exists(_provider.BuildRecordingFilePath(_currentPage.PageName));
            }
        }

        public bool IsRecordingAllowed
        {
            get { return IsReferenceAudioExists; }
        }

        public PlaybackSettings GetReferenceAudio()
        {
            if (!IsWord)
                return null;

            if (string.IsNullOrEmpty(_referenceAudioData))
            {
                _referenceAudioData = _audioDataLoader(GetPageAudioMethodName, 
                    new object[] { _useUkAudio ? FirstUkAudioCode : FirstUsAudioCode });
            }
            if (string.IsNullOrEmpty(_referenceAudioData))
                return null;

            return new PlaybackSettings(false, _referenceAudioData);
        }

        public PlaybackSettings GetRecordedAudio()
        {
            if (!IsWord)
                return null;

            string recordedFilePath = _provider.BuildRecordingFilePath(_currentPage.PageName);
            if (!File.Exists(recordedFilePath))
                return null;

            return new PlaybackSettings(true, recordedFilePath);
        }

        public RecordingSettings GetRecordingSettings()
        {
            if (!IsWord)
                return null;

            return new RecordingSettings(_provider.BuildRecordingFilePath(_currentPage.PageName));
        }

        private bool IsWord
        {
            get { return (_currentPage != null && _currentPage.IsWord); }
        }
    }
}
