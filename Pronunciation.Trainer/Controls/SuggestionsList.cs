using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Trainer.Controls;
using System.Windows.Controls;

namespace Pronunciation.Trainer.Controls
{
    public class SuggestionsList : ListBoxExt
    {
        private HashSet<object> _secondaryItemsLookup;

        public bool CanSelectPrevious
        {
            get { return GetPreviousItem(false, this.SelectedIndex) != null; }
        }

        public bool CanSelectNext
        {
            get { return GetNextItem(false, this.SelectedIndex) != null; }
        }

        public object SelectPreviousItem(bool setFocus, bool scrollIntoView)
        {
            var previousItem = GetPreviousItem(true, this.SelectedIndex);
            if (previousItem != null)
            {
                ChangeSelectedItemPresentation(setFocus, scrollIntoView);
            }

            return previousItem;
        }

        public object SelectNextItem(bool setFocus, bool scrollIntoView)
        {
            var nextItem = GetNextItem(true, this.SelectedIndex);
            if (nextItem != null)
            {
                ChangeSelectedItemPresentation(setFocus, scrollIntoView);
            }

            return nextItem;
        }

        public object SelectFirstItem(bool setFocus, bool scrollIntoView)
        {
            var firstItem = GetNextItem(true, -1);
            if (firstItem != null)
            {
                ChangeSelectedItemPresentation(setFocus, scrollIntoView);
            }

            return firstItem;
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

        public void AttachItemsSource<T>(IEnumerable<T> items)
        {
            AttachItemsSource<T>(items, null);
        }

        public void AttachItemsSource<T>(IEnumerable<T> mainItems, IEnumerable<T> secondaryItems)
        {
            if (secondaryItems == null)
            {
                this.ItemsSource = mainItems;
                _secondaryItemsLookup = null;
            }
            else
            {
                List<T> items = new List<T>(mainItems);
                _secondaryItemsLookup = new HashSet<object>();
                foreach (T secondaryItem in secondaryItems)
                {
                    items.Add(secondaryItem);
                    _secondaryItemsLookup.Add(secondaryItem);
                }
                this.ItemsSource = items;
            }
        }

        public bool IsSecondaryItem(object item)
        {
            return (item == null || _secondaryItemsLookup == null) ? false : _secondaryItemsLookup.Contains(item);
        }

        private object GetPreviousItem(bool changePosition, int initialPosition)
        {
            if (this.Items.Count <= 0 || initialPosition <= 0)
                return null;

            int previousPosition = initialPosition - 1;
            while (previousPosition >= 0)
            {
                var item = this.Items[previousPosition];
                if (IsSelectable(item))
                {
                    if (changePosition)
                    {
                        this.SelectedIndex = previousPosition;
                    }
                    return item;
                }

                previousPosition--;
            }

            return null;
        }

        private object GetNextItem(bool changePosition, int initialPosition)
        {
            if (this.Items.Count <= 0)
                return null;

            int nextPosition = initialPosition < 0 ? 0 : initialPosition + 1;
            while (nextPosition < this.Items.Count)
            {
                var item = this.Items[nextPosition];
                if (IsSelectable(item))
                {
                    if (changePosition)
                    {
                        this.SelectedIndex = nextPosition;
                    }
                    return item;
                }

                nextPosition++;
            }

            return null;
        }

        private void ChangeSelectedItemPresentation(bool setFocus, bool scrollIntoView)
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

        private bool IsSelectable(object item)
        {
            return !(item == null || ((item is ISuggestionItemInfo) && ((ISuggestionItemInfo)item).IsServiceItem));
        }
    }
}
