using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;

namespace Pronunciation.Trainer.Controls
{
    public class TabItemExt : TabItem
    {
        public Type DynamicContentType { get; set; }
        public Thickness DynamicContentMargin { get; set; }

        public void CaptureKeyboardFocus()
        {
            var content = this.Content as ISupportsKeyboardFocus;
            if (content != null && content.IsLoaded)
            {
                content.CaptureKeyboardFocus();
            } 
        }

        // This usually occurs when we click on the tab header while the tab content as active.
        // As a result, the content loses its focus so we pass the focus back
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
