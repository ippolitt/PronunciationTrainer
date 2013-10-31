using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Actions
{
    public class BackgroundActionSequence
    {
        private readonly BackgroundAction[] _actions;
        private int _currentIndex;
        private bool _isAborted;

        public BackgroundActionSequence(BackgroundAction[] actions)
        {
            _actions = actions;
        }

        public void StartNextAction(object contextData)
        {
            if (_isAborted || _currentIndex >= _actions.Length)
                return;

            _isAborted = !_actions[_currentIndex].StartAction(contextData, this);
            _currentIndex++;
        }
    }
}
