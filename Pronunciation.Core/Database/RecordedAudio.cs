namespace Pronunciation.Core.Database
{
    using System;
    using System.Collections.Generic;

    public partial class RecordedAudio
    {
        public Guid AudioId { get; set; }
        public byte[] RawData { get; set; }
        public DateTime Recorded { get; set; }
        public string TargetKey { get; set; }
        public int TargetTypeId { get; set; }
    }
}
