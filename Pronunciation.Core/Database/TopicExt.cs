using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Database
{
    public partial class Topic
    {
        public string TopicNameExt
        {
            get { return  BuildTopicName(TopicName, Level, Book);}
        }

        private static string BuildTopicName(string topicName, int? level, Book book)
        {
            if (!level.HasValue || level.Value <= 0)
                return topicName;

            return string.Format("{0}{1}", string.Concat(Enumerable.Repeat("   ", level.Value)), topicName);
        }
    }
}
