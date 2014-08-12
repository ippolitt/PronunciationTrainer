namespace Pronunciation.Core.Database
{
    using System;
    using System.Collections.Generic;
    
    public partial class ExerciseType
    {
        public ExerciseType()
        {
            this.Exercises = new HashSet<Exercise>();
        }
    
        public int ExerciseTypeId { get; set; }
        public string ExerciseTypeName { get; set; }
        public Nullable<int> Ordinal { get; set; }
    
        public virtual ICollection<Exercise> Exercises { get; set; }
    }
}
