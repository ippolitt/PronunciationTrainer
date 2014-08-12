using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Contexts;

namespace Pronunciation.Core.Providers.Recording
{
    public interface IRecordingProvider<T> where T : IRecordingTargetKey
    {
        bool ContainsAudios(T targetKey);
        PlaybackData GetAudio(T targetKey, string audioKey);
        PlaybackData GetLatestAudio(T targetKey);
        RecordedAudioListItem[] GetAudioList(T targetKey);

        RecordingSettings GetRecordingSettings(T targetKey);
        string RegisterNewAudio(T targetKey, DateTime recordingDate, string recordedFilePath,
            IRecordingHistoryPolicy recordingPolicy);

        bool DeleteAudios(T targetKey, IEnumerable<string> audioKeys);
        void DeleteTargetAudios(IEnumerable<T> targetKeys);
        bool MoveAudios<K>(T fromKey, K toKey, IEnumerable<string> audioKeys) where K : IRecordingTargetKey;
    }
}
