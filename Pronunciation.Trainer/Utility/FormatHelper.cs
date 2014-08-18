using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using Pronunciation.Core.Providers.Recording;

namespace Pronunciation.Trainer.Utility
{
    public static class FormatHelper
    {
        public static string ToTimeString(TimeSpan ts, bool allowMilliseconds)
        {
            int hours = Math.Abs(ts.Days * 24 + ts.Hours);
            int minutes = Math.Abs(ts.Minutes);
            double seconds = Math.Abs(ts.Seconds + (double)ts.Milliseconds / 1000);
            bool showMilliseconds = (allowMilliseconds && ts.TotalSeconds < 10);

            return string.Format("{0}{1}{2}:{3}",
                ts < TimeSpan.Zero ? "-" : null,
                hours > 0 ? string.Format("{0}:", hours) : null,
                string.Format(hours > 0 ? "{0:00}" : "{0:#0}", minutes),
                showMilliseconds
                    // use InvariantCulture to always have dot "." as a separator
                    ? string.Format(CultureInfo.InvariantCulture, "{0:00.#}", seconds)
                    : string.Format("{0:00}", Math.Ceiling(seconds)));
        }

        public static string ToTimeString(int durationMs, bool allowMilliseconds)
        {
            return ToTimeString(TimeSpan.FromMilliseconds(durationMs), allowMilliseconds);
        }
    }
}
