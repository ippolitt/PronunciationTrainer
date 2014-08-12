using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Providers.Recording.HistoryPolicies
{
    public class AlwaysOverrideRecordingPolicy : IRecordingHistoryPolicy
    {
        public bool OverrideLatestAudio(DateTime latestAudioDate)
        {
            return true;
        }
    }
}
