using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    class ContentCollector
    {
        private bool _isCollecting;
        private StringBuilder _collector;
        private int _hitCount;
        private int _nodeLevel;

        private readonly string _trackedNodeName;
        private readonly bool _firstOccurrenceOnly;

        public ContentCollector(string trackedNodeName, bool firstOccurrenceOnly)
        {
            _trackedNodeName = trackedNodeName;
            _firstOccurrenceOnly = firstOccurrenceOnly;
        }

        public void NodeOpened(string nodeName)
        {
            if (_isCollecting)
            {
                _nodeLevel++;
                return;
            }

            if (nodeName == _trackedNodeName && !(_firstOccurrenceOnly && _hitCount > 0))
            {
                _isCollecting = true;
                _nodeLevel = 1;
                _hitCount++;
            }
        }

        public void NodeClosed()
        {
            if (_isCollecting)
            {
                _nodeLevel--;
                if (_nodeLevel <= 0)
                {
                    _isCollecting = false;
                }
            }
        }

        public void Append(string content)
        {
            if (_isCollecting)
            {
                if (_collector == null)
                {
                    _collector = new StringBuilder();
                }
                _collector.Append(content);
            }
        }

        public string GetContent()
        {
            return (_collector == null ? null : _collector.ToString());
        }
    }
}
