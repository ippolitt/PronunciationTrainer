namespace Pronunciation.Core.Database
{
    using System;
    using System.Collections.Generic;
    
    public partial class Topic
    {
        public Topic()
        {
            this.Topic1 = new HashSet<Topic>();
            this.Exercises = new HashSet<Exercise>();
        }
    
        public int TopicId { get; set; }
        public string TopicName { get; set; }
        public Nullable<int> Level { get; set; }
        public Nullable<int> ParentTopicId { get; set; }
        public Nullable<int> BookId { get; set; }
        public Nullable<int> Ordinal { get; set; }
    
        public virtual Book Book { get; set; }
        public virtual ICollection<Topic> Topic1 { get; set; }
        public virtual Topic Topic2 { get; set; }
        public virtual ICollection<Exercise> Exercises { get; set; }
    }
}
