using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using Pronunciation.Trainer.Dictionary;
using System.Windows.Media;
using Pronunciation.Core.Providers.Dictionary;
using Pronunciation.Trainer.Controls;

namespace Pronunciation.Trainer.StyleSelectors
{
    public class SuggestionsListItemStyleSelector : StyleSelector
    {
        private readonly static Style _serviceItemStyle;
        private readonly static Style _extraItemStyle;
        private readonly static Style _multiPronItemStyle;

        static SuggestionsListItemStyleSelector()
        {
            _serviceItemStyle = new Style(typeof(ListBoxItem));
            _serviceItemStyle.Setters.Add(new Setter(ListBoxItem.ForegroundProperty, Brushes.Gray));
            _serviceItemStyle.Setters.Add(new Setter(ListBoxItem.FontStyleProperty, FontStyles.Italic));
            _serviceItemStyle.Setters.Add(new Setter(ListBoxItem.FocusableProperty, false));

            _extraItemStyle = new Style(typeof(ListBoxItem));
            _extraItemStyle.Setters.Add(new Setter(ListBoxItem.ForegroundProperty, Brushes.DarkSlateGray));

            _multiPronItemStyle = new Style(typeof(ListBoxItem));
            _multiPronItemStyle.Setters.Add(new Setter(ListBoxItem.FontWeightProperty, FontWeights.Bold));
        }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            Style st = null;
            if (item is IndexEntryImitation)
            {
                st = _serviceItemStyle;
            }
            else if (item is IndexEntry)
            {
                if (IsExtraItem((IndexEntry)item, container))
                {
                    st = _extraItemStyle;
                }
                else if (((IndexEntry)item).HasMultiplePronunciations == true)
                {
                    st = _multiPronItemStyle;
                }
            }

            return (st ?? base.SelectStyle(item, container));
        }

        private bool IsExtraItem(IndexEntry item, DependencyObject container)
        {
            if (item == null)
                return false;

            SuggestionsList lstSuggestions = ItemsControl.ItemsControlFromItemContainer(container) as SuggestionsList;
            return lstSuggestions == null ? false : lstSuggestions.IsSecondaryItem(item);
        }
    }
}
