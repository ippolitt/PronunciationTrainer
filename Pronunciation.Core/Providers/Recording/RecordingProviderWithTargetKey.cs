using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Contexts;

namespace Pronunciation.Core.Providers.Recording
{
    public abstract class RecordingProviderWithTargetKey
    {
        public abstract bool ContainsAudios();
        public abstract PlaybackData GetAudio(string audioKey);
        public abstract PlaybackData GetLatestAudio();
        public abstract RecordedAudioListItem[] GetAudioList();

        public abstract RecordingSettings GetRecordingSettings();
        public abstract string RegisterNewAudio(DateTime recordingDate, string recordedFilePath);

        public abstract bool DeleteAudios(IEnumerable<string> audioKeys);
        public abstract bool MoveAudios<K>(K toKey, IEnumerable<string> audioKeys) where K : IRecordingTargetKey;
    }

    public class RecordingProviderWithTargetKey<T> : RecordingProviderWithTargetKey where T : IRecordingTargetKey
    {
        private readonly T _targetKey;
        private readonly IRecordingProvider<T> _provider;
        private readonly IRecordingHistoryPolicy _policy;

        public RecordingProviderWithTargetKey(IRecordingProvider<T> provider, T targetKey, IRecordingHistoryPolicy policy)
        {
            _provider = provider;
            _targetKey = targetKey;
            _policy = policy;
        }

        public T TargetKey
        {
            get { return _targetKey; }
        }

        public override bool ContainsAudios()
        {
            return _provider.ContainsAudios(_targetKey);
        }

        public override PlaybackData GetAudio(string audioKey)
        {
            return _provider.GetAudio(_targetKey, audioKey);
        }

        public override PlaybackData GetLatestAudio()
        {
            return _provider.GetLatestAudio(_targetKey);
        }

        public override RecordedAudioListItem[] GetAudioList()
        {
            return _provider.GetAudioList(_targetKey);
        }

        public override RecordingSettings GetRecordingSettings()
        {
            return _provider.GetRecordingSettings(_targetKey);
        }

        public override string RegisterNewAudio(DateTime recordingDate, string recordedFilePath)
        {
            return _provider.RegisterNewAudio(_targetKey, recordingDate, recordedFilePath, _policy);
        }

        public override bool DeleteAudios(IEnumerable<string> audioKeys)
        {
            return _provider.DeleteAudios(_targetKey, audioKeys);
        }

        public override bool MoveAudios<K>(K toKey, IEnumerable<string> audioKeys)
        {
            return _provider.MoveAudios(_targetKey, toKey, audioKeys);
        }
    }
}
