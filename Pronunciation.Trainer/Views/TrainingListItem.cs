using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Database;

namespace Pronunciation.Trainer.Views
{
    public class TrainingListItem
    {
        public Guid TrainingId { get; set; }
        public string Title { get; set; }
        public DateTime? Created { get; set; }
        public string Notes { get; set; }
        public string Category { get; set; }
        public int? CharacterCount { get; set; }
        public int? ReferenceAudioDuration { get; set; }
    }
}
