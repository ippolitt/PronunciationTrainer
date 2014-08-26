//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
namespace Pronunciation.Core.Database
{
    using System;
    using System.Collections.Generic;
    
    public partial class Exercise
    {
        public Exercise()
        {
            this.ExerciseAudios = new HashSet<ExerciseAudio>();
        }

        public Guid ExerciseId { get; set; }
        public Nullable<int> ExerciseTypeId { get; set; }
        public Nullable<int> BookId { get; set; }
        public Nullable<int> TopicId { get; set; }
        public string Title { get; set; }
        public string TargetSound { get; set; }
        public string ExecutionNotes { get; set; }
        public Nullable<int> SourcePage { get; set; }
        public Nullable<int> SourceCD { get; set; }
        public Nullable<int> SourceTrack { get; set; }
        public byte[] ExerciseData { get; set; }
    
        public virtual Book Book { get; set; }
        public virtual ExerciseType ExerciseType { get; set; }
        public virtual Topic Topic { get; set; }
        public virtual ICollection<ExerciseAudio> ExerciseAudios { get; set; }
    }
}
