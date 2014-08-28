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
            FocusSelectedItemInternal(true);
        }

        public void ScrollToSelectedItem()
        {
            if (this.SelectedItem == null)
                return;

            // If ItemsSource has recently changed it may throw an exception so we do this call via Dispatcher
            this.Dispatcher.BeginInvoke(new Action(ScrollToSelectedItemInternal));
        }

        public bool SelectItem(object item, bool setFocus, bool scrollIntoView)
        {
            if (item == null || this.Items.Count <= 0)
                return false;

            if (!ReferenceEquals(this.SelectedItem, item))
            {
                this.SelectedItem = item;
            }

            if (ReferenceEquals(this.SelectedItem, item))
            {
                ChangeSelectedItemPresentation(setFocus, scrollIntoView);
                return true;
            }

            return false;
        }

        protected void ChangeSelectedItemPresentation(bool setFocus, bool scrollIntoView)
        {
            if (scrollIntoView)
            {
                // Scroll should be before Focus (for some reason it works better)
                this.ScrollToSelectedItem();
            }
            if (setFocus)
            {
                this.FocusSelectedItem();
            }
        }

        private void ScrollToSelectedItemInternal()
        {
            if (this.SelectedItem == null)
                return;

            try
            {
                this.ScrollIntoView(this.SelectedItem);
            }
            catch 
            {
                // Errors are still possible sometimes
            }
        }

        private void FocusSelectedItemInternal(bool allowRetry)
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
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => FocusSelectedItemInternal(false)));
                }
                else
                {
                    this.Focus();
                }
            }
        }
    }
}
