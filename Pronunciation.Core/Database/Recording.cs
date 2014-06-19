using System;
using System.Collections.Generic;

namespace Pronunciation.Core.Database
{
    public partial class Recording
    {
        public System.Guid RecordingId { get; set; }
        public string RecordingText { get; set; }
        public string Title { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public string Notes { get; set; }
        public string Category { get; set; }
    }
}
