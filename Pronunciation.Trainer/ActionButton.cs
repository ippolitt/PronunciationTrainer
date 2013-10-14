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
        private object _originalContent;
        private BackgroundAction _target;

        public string StopText { get; set; }
        public bool SupportsAbort { get; set; }

        public static readonly DependencyProperty IsRunningProperty = DependencyProperty.Register(
            "IsRunning", typeof(bool), typeof(ActionButton));

        public bool IsRunning
        {
            get { return (bool)GetValue(IsRunningProperty); }
            set { SetValue(IsRunningProperty, value); }
        }

        public ActionButton()
        {
            DataContext = this;
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
            IsRunning = true;
            IsEnabled = SupportsAbort;
            if (SupportsAbort && !string.IsNullOrEmpty(StopText))
            {
                _originalContent = Content;
                Content = StopText;
            }
        }

        private void Target_ActionCompleted(BackgroundAction action)
        {
            IsRunning = false;
            IsEnabled = true;
            if (_originalContent != null)
            {
                Content = _originalContent;
            }
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
