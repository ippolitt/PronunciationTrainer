using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Trainer.Views
{
    public class ExerciseAudioListItemWithData : ExerciseAudioListItem
    {
        public byte[] RawData { get; set; }
    }
}
