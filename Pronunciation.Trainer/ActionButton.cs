using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.ComponentModel;
using Pronunciation.Core.Actions;
using System.Windows.Input;
using System.Windows;

namespace Pronunciation.Trainer
{
    public class ActionButton : Button
    {
        private string _originalText;
        private BackgroundAction _target;

        public string StopText { get; set; }
        public bool SupportsAbort { get; set; }

        private const string DefaultStopText = "Stop";

        public ActionButton()
        {
            //AccessKeyManager.AddAccessKeyPressedHandler(this, ActionButton_AccessKeyPressed);
        }

        protected override void OnAccessKey(AccessKeyEventArgs e)
        {
            // Ignore access keys pressed without Alt modifier
            if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                base.OnAccessKey(e);
            }
        }

        [Browsable(false)]
        public BackgroundAction Target
        {
            get
            {
                return _target;
            }
            set
            {
                _target = value;
                if (_target != null)
                {
                    _target.ActionStarted -= Target_ActionStarted;
                    _target.ActionStarted += Target_ActionStarted;

                    _target.ActionCompleted -= Target_ActionCompleted;
                    _target.ActionCompleted += Target_ActionCompleted;
                }
            }
        }

        private void Target_ActionStarted(BackgroundAction action)
        {
            _originalText = (string)Content;

            if (SupportsAbort)
            {
                Content = string.IsNullOrWhiteSpace(StopText) ? DefaultStopText : StopText;
                IsEnabled = true;
            }
            else
            {
                IsEnabled = false;
            }
        }

        private void Target_ActionCompleted(BackgroundAction action)
        {
            IsEnabled = true;
            Content = _originalText;
        }

        protected override void OnClick()
        {
            if (Target.IsRunning)
            {
                if (SupportsAbort)
                {
                    Target.RequestAbort(true);
                }
            }
            else
            {
                Target.StartAction();
            }

            base.OnClick();
        }
    }
}
