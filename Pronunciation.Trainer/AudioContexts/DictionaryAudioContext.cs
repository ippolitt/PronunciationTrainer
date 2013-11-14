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
        private IndexEntry _currentIndex;
        private string _referenceAudioData;
        private bool _useUkAudio;

        private const string GetPageAudioMethodName = "extGetAudioData";
        private const string GetPageAudioByKeyMethodName = "extGetAudioByKey";
        private const int FirstUkAudioCode = 1;
        private const int FirstUsAudioCode = 2;

        public event AudioContextChangedHandler ContextChanged;
        public delegate string GetPageAudioHandler(string methodName, object[] methodArgs);

        public DictionaryAudioContext(LPDProvider provider, GetPageAudioHandler audioDataLoader)
        {
            _provider = provider;
            _audioDataLoader = audioDataLoader;
        }

        public void RefreshContext(IndexEntry currentIndex, bool useUkAudio, bool playImmediately)
        {
            _referenceAudioData = null;
            _useUkAudio = useUkAudio;
            _currentIndex = currentIndex;

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

        public bool IsReferenceAudioExists
        {
            get 
            {
                if (_currentIndex == null)
                    return false;

                if (!string.IsNullOrEmpty(_referenceAudioData))
                    return true;

                return (_useUkAudio && !string.IsNullOrEmpty(_currentIndex.SoundKeyUK))
                    || (!_useUkAudio && !string.IsNullOrEmpty(_currentIndex.SoundKeyUS)); 
            }
        }

        public bool IsRecordedAudioExists
        {
            get
            {
                if (_currentIndex == null)
                    return false;

                return File.Exists(_provider.BuildRecordingFilePath(_currentIndex.Key));
            }
        }

        public bool IsRecordingAllowed
        {
            get { return IsReferenceAudioExists; }
        }

        public PlaybackSettings GetReferenceAudio()
        {
            if (_currentIndex == null)
                return null;

            if (string.IsNullOrEmpty(_referenceAudioData))
            {
                var audioKey = _useUkAudio ? _currentIndex.SoundKeyUK : _currentIndex.SoundKeyUS;
                if (!string.IsNullOrEmpty(audioKey))
                {
                    _referenceAudioData = _audioDataLoader(GetPageAudioByKeyMethodName, new object[] { audioKey });
                }

                //if (string.IsNullOrEmpty(_referenceAudioData))
                //{
                //    _referenceAudioData = _audioDataLoader(GetPageAudioMethodName,
                //        new object[] { _useUkAudio ? FirstUkAudioCode : FirstUsAudioCode });
                //}
            }
            if (string.IsNullOrEmpty(_referenceAudioData))
                return null;

            return new PlaybackSettings(false, _referenceAudioData);
        }

        public PlaybackSettings GetRecordedAudio()
        {
            if (_currentIndex == null)
                return null;

            string recordedFilePath = _provider.BuildRecordingFilePath(_currentIndex.Key);
            if (!File.Exists(recordedFilePath))
                return null;

            return new PlaybackSettings(true, recordedFilePath);
        }

        public RecordingSettings GetRecordingSettings()
        {
            if (_currentIndex == null)
                return null;

            return new RecordingSettings(_provider.BuildRecordingFilePath(_currentIndex.Key));
        }
    }
}
