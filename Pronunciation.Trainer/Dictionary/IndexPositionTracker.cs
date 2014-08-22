using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Providers.Dictionary;

namespace Pronunciation.Trainer.Dictionary
{
    public class IndexPositionTracker
    {
        private readonly DictionaryIndex _targetIndex;
        private int _currentPosition;
        private IndexEntry _currentEntry;

        public IndexPositionTracker(DictionaryIndex targetIndex)
        {
            _targetIndex = targetIndex;
            _currentPosition = -1;
        }

        public bool CanGoPrevious
        {
            get { return _currentPosition > 0; }
        }

        public bool CanGoNext
        {
            get { return (_targetIndex.EntriesCount > 0 && _currentPosition < _targetIndex.EntriesCount - 1); }
        }

        public IndexEntry GoPrevious()
        {
            if (!CanGoPrevious)
                return null;

            var entry = _targetIndex.GetEntryByPosition(_currentPosition - 1);
            if (entry != null)
            {
                _currentPosition--;
                _currentEntry = entry;
            }

            return entry;
        }

        public IndexEntry GoNext()
        {
            if (!CanGoNext)
                return null;

            var entry = _targetIndex.GetEntryByPosition(_currentPosition + 1);
            if (entry != null)
            {
                _currentPosition++;
                _currentEntry = entry;
            }

            return entry;
        }

        public bool RewindToEntry(IndexEntry entry)
        {
            if (entry == null)
                return false;

            if (_currentEntry != null && ReferenceEquals(_currentEntry, entry))
                return true;

            int entryPosition = _targetIndex.GetEntryPosition(entry);
            if (entryPosition < 0)
                return false;

            _currentPosition = entryPosition;
            _currentEntry = entry;
            return true;
        }
    }
}
