using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;

namespace Pronunciation.Trainer.Controls
{
    public class ComboBoxExt : ComboBox
    {
        public bool DisableAltGestures { get; set; }

        public ComboBoxExt()
        {
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (DisableAltGestures && (e.KeyboardDevice.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                return;
            }

            base.OnKeyDown(e);
        }
    }
}
