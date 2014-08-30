using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pronunciation.Core.Providers.Theory
{
    public class TheoryProvider
    {
        private readonly string _topicsFolder;
        private const string TopicsFolderName = "Topics";

        public TheoryProvider(string baseFolder)
        {
            _topicsFolder = Path.Combine(baseFolder, TopicsFolderName);
        }

        public TheoryTopicInfo[] GetTopics()
        {
            return Directory.GetFiles(_topicsFolder, "*.png", SearchOption.TopDirectoryOnly)
                .Select(x => new TheoryTopicInfo(Path.GetFileNameWithoutExtension(x), x)).ToArray();
        }

        public byte[] GetTopicContent(TheoryTopicInfo topic)
        {
            if (!File.Exists(topic.SourceFilePath))
                throw new ArgumentException(string.Format("Content for topic '{0}' doesn't exist!", topic.TopicName));

            return File.ReadAllBytes(topic.SourceFilePath);
        }
    }
}
