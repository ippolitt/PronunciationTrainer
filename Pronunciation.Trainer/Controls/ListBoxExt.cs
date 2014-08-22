using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Pronunciation.Trainer.Controls
{
    public class ListBoxExt : ListBox
    {
        public void FocusSelectedItem()
        {
            FocusSelectedItem(true);
        }

        private void FocusSelectedItem(bool allowRetry)
        {
            if (this.SelectedIndex < 0)
            {
                this.Focus();
                return;
            }

            // We must put focus on the selected item, not on the list itself 
            // otherwise list navigation with arrows may break
            var item = (ListBoxItem)this.ItemContainerGenerator.ContainerFromIndex(this.SelectedIndex);
            if (item != null)
            {
                item.Focus();
            }
            else
            {
                if (allowRetry)
                {
                    // Retry asynchrously with low priority so that ItemContainerGenerator was able to generate all required items
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => FocusSelectedItem(false)));
                }
                else
                {
                    this.Focus();
                }
            }
        }
    }
}
