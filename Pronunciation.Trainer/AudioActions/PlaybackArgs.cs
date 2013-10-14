using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Trainer.AudioActions
{
    public class PlaybackArgs
    {
        public bool IsReferenceAudio;
        public bool IsFilePath;
        public string PlaybackData;
        public float PlaybackVolumeDb;
        public int SkipMs;
    }
}
