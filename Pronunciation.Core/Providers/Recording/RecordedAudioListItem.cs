using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Providers.Recording
{
    public class RecordedAudioListItem
    {
        public string AudioKey { get; set; }
        public DateTime RecordingDate { get; set; }

        public string Text
        {
            get { return RecordingDate.ToString("yyyy-MM-dd HH-mm-ss"); }
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
