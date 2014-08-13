using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Providers.Recording
{
    public interface IRecordingHistoryPolicy
    {
        bool OverrideLatestAudio(DateTime recordingFate, DateTime latestAudioDate);
    }
}
