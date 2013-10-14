using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;

namespace Pronunciation.Trainer
{
    public class TabItemExt : TabItem
    {
        public void CaptureKeyboardFocus()
        {
            var content = this.Content as ISupportsKeyboardFocus;
            if (content != null && content.IsLoaded)
            {
                content.CaptureKeyboardFocus();
            } 
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);

            var content = this.Content as ISupportsKeyboardFocus;
            if (content == null)
                return;

            // Pass keyboard focus over to the contained control
            if (content.IsLoaded && ReferenceEquals(Keyboard.FocusedElement, this))
            {
                content.CaptureKeyboardFocus();
            } 
        }
    }
}
