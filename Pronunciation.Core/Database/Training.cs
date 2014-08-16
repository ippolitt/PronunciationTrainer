namespace Pronunciation.Core.Database
{
    using System;
    using System.Collections.Generic;

    public partial class Training
    {
        public Guid TrainingId { get; set; }
        public string TrainingText { get; set; }
        public byte[] TrainingData { get; set; }
        public string Title { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public string Notes { get; set; }
        public string Category { get; set; }
        public byte[] ReferenceAudioData { get; set; }
        public string ReferenceAudioName { get; set; }
        public Nullable<System.Int32> CharacterCount { get; set; }
    }
}
