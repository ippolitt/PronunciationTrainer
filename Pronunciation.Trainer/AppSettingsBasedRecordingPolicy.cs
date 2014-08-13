using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Providers.Recording;

namespace Pronunciation.Trainer
{
    public class AppSettingsBasedRecordingPolicy : IRecordingHistoryPolicy
    {
        public bool OverrideLatestAudio(DateTime recordingDate, DateTime latestAudioDate)
        {
            bool isOverride;
            switch (AppSettings.Instance.HistoryMode)
            {
                case RecordingHistoryMode.AlwaysAdd:
                    isOverride = false;
                    break;
                case RecordingHistoryMode.AlwaysOverrideLatest:
                    isOverride = true;
                    break;
                case RecordingHistoryMode.OverrideLatestAfterNDays:
                    int historyDays = AppSettings.Instance.HistoryDays;
                    if (historyDays <= 0)
                    {
                        isOverride = false;
                    }
                    else
                    {
                        isOverride = recordingDate.Subtract(latestAudioDate) < TimeSpan.FromDays(historyDays);
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }

            return isOverride;
        }
    }
}
