using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Trainer.Views
{
    public class ExerciseAudioListItem
    {
        public Guid AudioId { get; set; }
        public string AudioName { get; set; }
        public Guid ExerciseId { get; set; }

        public string Text
        {
            get { return AudioName; }
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
