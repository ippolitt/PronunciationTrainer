using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using Pronunciation.Trainer.ValueConverters;

namespace Pronunciation.Trainer.Controls
{
    public class OnOffButton : Button
    {
        public string StateOnTooltip { get; set; }
        public string StateOffTooltip { get; set; }

        public object[] StateOnTooltipArgs { get; set; }
        public object[] StateOffTooltipArgs { get; set; }

        public static readonly DependencyProperty IsOnProperty = DependencyProperty.Register(
            "IsOn", typeof(bool), typeof(OnOffButton));

        public bool IsStateOn 
        {
            get 
            { 
                return (bool)GetValue(IsOnProperty); 
            }
            set 
            {
                SetValue(IsOnProperty, value);
                RefreshTooltip();
            }
        }
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            RefreshTooltip();
        }

        public void RefreshTooltip()
        {
            if (IsStateOn)
            {
                this.ToolTip = StateOnTooltipArgs == null ? StateOnTooltip : string.Format(StateOnTooltip, StateOnTooltipArgs);
            }
            else
            {
                this.ToolTip = StateOffTooltipArgs == null ? StateOffTooltip : string.Format(StateOffTooltip, StateOffTooltipArgs);
            }
        }
    }
}
