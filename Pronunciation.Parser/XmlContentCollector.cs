using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    class XmlContentCollector
    {
        private readonly string _trackedNodeName;
        private readonly bool _firstOccurrenceOnly;
        private readonly StringBuilder _collector;

        private List<string> _results;
        private bool _isCollecting;
        private int _hitCount;
        private int _nodeLevel;

        public XmlContentCollector(string trackedNodeName, bool firstOccurrenceOnly)
        {
            _trackedNodeName = trackedNodeName;
            _firstOccurrenceOnly = firstOccurrenceOnly;
            _collector = new StringBuilder();
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
                    FlushCollector();
                }
            }
        }

        public void Append(string content)
        {
            if (_isCollecting)
            {
                _collector.Append(content);
            }
        }

        public string[] GetContent()
        {
            FlushCollector();

            return (_results == null || _results.Count == 0 ? null : _results.ToArray());
        }

        private void FlushCollector()
        {
            if (_collector.Length > 0)
            {
                if (_results == null)
                {
                    _results = new List<string>();
                }
                _results.Add(_collector.ToString());
                _collector.Clear();
            }
        }
    }
}
