using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.ComponentModel;
using Pronunciation.Core.Actions;
using System.Windows.Input;
using System.Windows;

namespace Pronunciation.Trainer.Controls
{
    public class ActionButton : Button
    {
        public enum ActionButtonState
        {
            Stopped,
            Running,
            Paused
        }

        private BackgroundAction _target;

        public string DefaultTooltip { get; set; }
        public string RunningTooltip { get; set; }
        public string PausedTooltip { get; set; }

        public static readonly DependencyProperty ButtonStateProperty = DependencyProperty.Register(
            "ButtonState", typeof(ActionButtonState), typeof(ActionButton));
        public static readonly DependencyProperty DynamicTooltipProperty = DependencyProperty.Register(
            "DynamicTooltip", typeof(string), typeof(ActionButton));

        public ActionButton()
        {
            DataContext = this;
        }

        public void RefreshDefaultTooltip()
        {
            SetValue(DynamicTooltipProperty, DefaultTooltip);
        }

        protected override void OnInitialized(EventArgs e)
        {
            SetValue(DynamicTooltipProperty, DefaultTooltip);
            base.OnInitialized(e);
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

                    _target.ActionStateChanged -= Target_ActionStateChanged;
                    _target.ActionStateChanged += Target_ActionStateChanged;
                }
            }
        }

        private void Target_ActionStateChanged(BackgroundAction action)
        {
            ActionButtonState state;
            string tooltip;
            switch (action.ActionState)
            {
                case BackgroundActionState.Running:
                    state = ActionButtonState.Running;
                    tooltip = string.IsNullOrEmpty(RunningTooltip) ? DefaultTooltip : RunningTooltip;
                    break;

                case BackgroundActionState.Suspended:
                    state = ActionButtonState.Paused;
                    tooltip = string.IsNullOrEmpty(PausedTooltip) ? DefaultTooltip : PausedTooltip;
                    break;

                default:
                    state = ActionButtonState.Stopped;
                    tooltip = DefaultTooltip;
                    break;
            }

            SetValue(ButtonStateProperty, state);
            SetValue(DynamicTooltipProperty, tooltip);
        }

        private void Target_ActionStarted(BackgroundAction action)
        {
            IsEnabled = action.IsAbortable;
        }

        private void Target_ActionCompleted(BackgroundAction action)
        {
            IsEnabled = true;
        }

        protected override void OnClick()
        {
            switch (Target.ActionState)
            {
                case BackgroundActionState.Suspended:
                    Target.Resume();
                    break;

                case BackgroundActionState.Running:
                    if (Target.IsSuspendable)
                    {
                        Target.Suspend();
                    }
                    else if (Target.IsAbortable)
                    {
                        Target.RequestAbort(true);
                    }
                    else
                    {
                        // do nothing - just wait until action completes
                    }
                    break;

                default:
                    Target.StartAction();
                    break;
            }

            base.OnClick();
        }
    }
}
