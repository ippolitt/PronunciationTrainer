using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Pronunciation.Trainer.Commands
{
    public class ExecuteActionCommand : ICommand
    {
        private readonly Action _target;
        private bool _canExecute;

        public event EventHandler CanExecuteChanged;

        public ExecuteActionCommand(Action target, bool canExecute)
        {
            if (target == null)
                throw new ArgumentNullException();

            _target = target;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute;
        }

        public void Execute(object parameter)
        {
            _target();
        }

        public void UpdateState(bool canExecute)
        {
            if (_canExecute != canExecute)
            {
                _canExecute = canExecute;
                if (CanExecuteChanged != null)
                {
                    CanExecuteChanged(this, null);
                }
            }
        }
    }
}
