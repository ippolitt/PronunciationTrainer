using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Trainer.Views
{
    public class RecordingLight
    {
        public System.Guid RecordingId { get; set; }
        public string Title { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public string Notes { get; set; }
        public string Category { get; set; }
    }
}
