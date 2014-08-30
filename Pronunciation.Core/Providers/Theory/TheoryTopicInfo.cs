using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Providers.Theory
{
    public class TheoryTopicInfo
    {
        public string TopicName { get; private set; }
        public string SourceFilePath { get; private set; }

        public TheoryTopicInfo(string topicName, string sourceFilePath)
        {
            TopicName = topicName;
            SourceFilePath = sourceFilePath;
        }
    }
}
