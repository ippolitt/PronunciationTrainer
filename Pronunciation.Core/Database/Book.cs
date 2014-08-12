namespace Pronunciation.Core.Database
{
    using System;
    using System.Collections.Generic;
    
    public partial class Book
    {
        public Book()
        {
            this.Topics = new HashSet<Topic>();
            this.Exercises = new HashSet<Exercise>();
        }
    
        public int BookId { get; set; }
        public string Title { get; set; }
        public string ShortName { get; set; }
        public string Author { get; set; }
    
        public virtual ICollection<Topic> Topics { get; set; }
        public virtual ICollection<Exercise> Exercises { get; set; }
    }
}
