using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Providers.Recording.HistoryPolicies
{
    public class AlwaysAddRecordingPolicy : IRecordingHistoryPolicy
    {
        public bool OverrideLatestAudio(DateTime recordingFate, DateTime latestAudioDate)
        {
            return false;
        }
    }
}
