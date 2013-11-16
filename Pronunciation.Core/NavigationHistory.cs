using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core
{
    public class NavigationHistory<T>
    {
        private class LinkedListItem<TValue>
        {
            public LinkedListItem<TValue> Previous;
            public LinkedListItem<TValue> Next;
            public TValue Value { get; private set; }

            public LinkedListItem(TValue value)
            {
                Value = value;
            }
        }

        private LinkedListItem<T> _currentNode;
        private EqualityComparer<T> _comparer = EqualityComparer<T>.Default;

        public bool CanGoBack
        {
            get { return GetPreviousNode() != null; }
        }

        public bool CanGoForward
        {
            get { return GetNextNode() != null; }
        }

        public T GoBack()
        {
            _currentNode = GetPreviousNode();
            return _currentNode == null ? default(T) : _currentNode.Value;
        }

        public T GoForward()
        {
            _currentNode = GetNextNode();
            return _currentNode == null ? default(T) : _currentNode.Value;
        }

        public void RegisterPage(T page)
        {
            if (_currentNode == null)
            {
                _currentNode = new LinkedListItem<T>(page);
            }
            else
            {
                if (_comparer.Equals(_currentNode.Value, page))
                    return;

                var nextNode = new LinkedListItem<T>(page);
                nextNode.Previous = _currentNode;
                _currentNode.Next = nextNode;

                _currentNode = nextNode;
            }
        }

        private LinkedListItem<T> GetPreviousNode()
        {
            return _currentNode == null ? null : _currentNode.Previous;
        }

        private LinkedListItem<T> GetNextNode()
        {
            return _currentNode == null ? null : _currentNode.Next;
        }
    }
}
