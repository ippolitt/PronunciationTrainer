using System;
using System.Collections.Generic;

namespace Pronunciation.Trainer.Views
{
    public partial class ExerciseListItem
    {
        public Guid ExerciseId { get; set; }
        public string Title { get; set; }
        public string TargetSound { get; set; }
        public Nullable<int> SourcePage { get; set; }
        public Nullable<int> SourceCD { get; set; }
        public Nullable<int> SourceTrack { get; set; }

        public Nullable<int> ExerciseTypeId { get; set; }
        public Nullable<int> BookId { get; set; }
        public Nullable<int> TopicId { get; set; }

        public string TrackDisplayName
        {
            get { return string.Format("{0:00}-{1:00}", SourceCD, SourceTrack); }
        }
    }
}
