using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Database;
using System.IO;

namespace Pronunciation.Core.Parsers
{
    public class TopicParser
    {
        private readonly string _topicsFilePath;

        public TopicParser(string topicsFilePath)
        {
            _topicsFilePath = topicsFilePath;
            if (!File.Exists(_topicsFilePath))
                throw new ArgumentException();
        }

        public void ImportTopics()
        {
            var rawTopics = File.ReadAllLines(_topicsFilePath);
            Entities context = new Entities();

            var topics = new List<Topic>();
            int topicId = 0;
            if (context.Topics.Any())
            {
                topicId = context.Topics.Max(x => x.TopicId);
            }
            foreach (var rawTopic in rawTopics.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                int level = 0;
                foreach (char ch in rawTopic)
                {
                    if (ch != '\t')
                        break;

                    level++;
                }

                Topic parent = null;
                if (level > 0)
                {
                    parent = FindParentTopic(level, topics);
                }

                topicId++;
                topics.Add(new Topic
                {
                    TopicId = topicId,
                    TopicName = PrepareTopicName(rawTopic),
                    Level = level,
                    ParentTopicId = parent == null ? (int?)null : parent.TopicId
                });
            }

            foreach (var topic in topics)
            {
                context.Topics.Add(topic);
            }
            context.SaveChanges();
        }

        private static Topic FindParentTopic(int currentLevel, List<Topic> topics)
        {
            Topic parent;
            for (int i = topics.Count - 1; i >= 0; i--)
            {
                parent = topics[i];
                if (parent.Level == currentLevel - 1)
                    return parent;
            }

            throw new ArgumentException();
        }

        private static string PrepareTopicName(string name)
        {
            string result = name.Trim();

            int lastSpace = result.LastIndexOf(' ');
            if (lastSpace >= 0)
            {
                var lastSegment = result.Substring(lastSpace + 1);
                int pageNumber;
                if (Int32.TryParse(lastSegment, out pageNumber))
                {
                    result = result.Remove(lastSpace).TrimEnd();
                }
            }

            if (string.IsNullOrWhiteSpace(result))
                throw new ArgumentNullException();

            return result;
        }
    }
}
