using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Database
{
    public partial class DictionaryCollocation
    {
        public int CollocationId { get; set; }
        public string CollocationText { get; set; }
        public int WordId { get; set; }
        public string SoundKeyUK { get; set; }
        public string SoundKeyUS { get; set; }

        public virtual DictionaryWord DictionaryWord { get; set; }
    }
}
