using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core
{
    public class CollectionChangeTracker<T>
    {
        private readonly HashSet<T> _deletedItems;
        private readonly HashSet<T> _addedItems;

        public CollectionChangeTracker()
        {
            _deletedItems = new HashSet<T>();
            _addedItems = new HashSet<T>();
        }

        public CollectionChangeTracker(IEqualityComparer<T> comparer)
        {
            _deletedItems = new HashSet<T>(comparer);
            _addedItems = new HashSet<T>(comparer);
        }

        public void RegisterDeletedItem(T item)
        {
            _deletedItems.Add(item);
        }

        public void RegisterAddedItem(T item)
        {
             _addedItems.Add(item);
        }

        public void UnregisterDeletedItem(T item)
        {
            _deletedItems.Remove(item);
        }

        public void UnregisterAddedItem(T item)
        {
            _addedItems.Remove(item);
        }

        public bool HasDeletedItems
        {
            get { return _deletedItems.Count > 0; }
        }

        public bool HasAddedItems
        {
            get { return _addedItems.Count > 0; }
        }

        public T[] GetDeletedItems()
        {
            return _deletedItems.ToArray();
        }

        public T[] GetAddedItems()
        {
            return _addedItems.ToArray();
        }

        public void Reset()
        {
            _deletedItems.Clear();
            _addedItems.Clear();
        }

        public void RegisterDeletedItems(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                RegisterDeletedItem(item);
            }
        }

        public void RegisterAddedItems(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                RegisterAddedItem(item);
            }
        }
    }
}
