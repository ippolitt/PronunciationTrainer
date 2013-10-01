using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation
{

    class StructureTracker
    {
        private enum EntryState
        {
            Undefined = 0,
            EntryNumber,
            MainEntry,
            Comment,
            WordForm,
            Collocation,
            Image
        }

        private string _curKeyword;
        private bool _isActiveKeyword;

        private string _entryNumber;
        private int _entriesCount;
        private EntryState _state;

        public int KeywordsCount { get; private set; }

        public string EntryNumber
        {
            get { return _entryNumber; }
        }

        public string Keyword
        {
            get { return _curKeyword; }
        }

        public void RegisterKeyword(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
                throw new ArgumentNullException();

            if (string.Equals(_curKeyword, keyword, StringComparison.Ordinal))
                throw new Exception(string.Format("The word '{0}' is already registered!", keyword));

            if (_state == EntryState.Undefined && _isActiveKeyword)
                throw new Exception(string.Format("Previous word '{0}' doesn't have any entries!", _curKeyword));

            if (_state == EntryState.EntryNumber)
                throw new Exception(string.Format("New word can't be registered after entry number!"));

            _entriesCount = 0;
            _entryNumber = null;
            _state = EntryState.Undefined;
            _isActiveKeyword = true;
            _curKeyword = keyword;
            KeywordsCount++;
        }

        public void RegisterEntryNumber(string number)
        {
            if (!_isActiveKeyword)
                throw new Exception(string.Format("The word must be registered first!"));

            _entryNumber = number;
            _state = EntryState.EntryNumber;
        }

        public void RegisterEntry()
        {
            if (!_isActiveKeyword)
                throw new Exception(string.Format("The word must be registered first!"));

            if (_entriesCount == 0 && !(_state == EntryState.Undefined || _state == EntryState.EntryNumber))
                throw new Exception(string.Format("The first entry must be registered immediately after the word or the entry number!"));

            if (_entriesCount > 0 && _state != EntryState.EntryNumber)
                throw new Exception(string.Format("The subsequent entry must be registered immediately after the entry number!"));

            _entriesCount++;
            _state = EntryState.MainEntry;
        }

        public void RegisterComment()
        {
            if (_state == EntryState.Undefined || _state == EntryState.EntryNumber)
                throw new Exception(string.Format("Invalid order of comment!"));

            _state = EntryState.Comment;
        }

        public void RegisterEntryForm()
        {
            if (_state == EntryState.Undefined || _state == EntryState.EntryNumber)
                throw new Exception(string.Format("Invalid order of word form!"));

            _state = EntryState.WordForm;
        }

        public void RegisterEntryCollocation()
        {
            if (_state == EntryState.Undefined || _state == EntryState.EntryNumber)
                throw new Exception(string.Format("Invalid order of collocation!"));

            _state = EntryState.Collocation;
        }

        public void RegisterImage()
        {
            if (_state == EntryState.Undefined || _state == EntryState.EntryNumber || _state == EntryState.Image)
                throw new Exception(string.Format("Invalid order of image!"));

            _state = EntryState.Image;
        }
    }
}
