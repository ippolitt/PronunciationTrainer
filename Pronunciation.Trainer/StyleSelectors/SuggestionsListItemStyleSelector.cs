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
        private readonly static Style _collocationItemStyle;
        private readonly static Style _extraItemNormalStyle;
        private readonly static Style _extraItemCollocationStyle;

        static SuggestionsListItemStyleSelector()
        {
            _serviceItemStyle = new Style(typeof(ListBoxItem));
            _serviceItemStyle.Setters.Add(new Setter(ListBoxItem.ForegroundProperty, Brushes.Gray));
            _serviceItemStyle.Setters.Add(new Setter(ListBoxItem.FontStyleProperty, FontStyles.Italic));
            _serviceItemStyle.Setters.Add(new Setter(ListBoxItem.FocusableProperty, false));

            _collocationItemStyle = new Style(typeof(ListBoxItem));
            _collocationItemStyle.Setters.Add(new Setter(ListBoxItem.FontStyleProperty, FontStyles.Italic));

            _extraItemNormalStyle = new Style(typeof(ListBoxItem));
            _extraItemNormalStyle.Setters.Add(new Setter(ListBoxItem.ForegroundProperty, Brushes.DarkSlateGray));

            _extraItemCollocationStyle = new Style(typeof(ListBoxItem), _extraItemNormalStyle);
            _extraItemCollocationStyle.Setters.Add(new Setter(ListBoxItem.FontStyleProperty, FontStyles.Italic));
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
                bool isCollocation = ((IndexEntry)item).IsCollocation;
                bool isExtraItem = IsExtraItem((IndexEntry)item, container);
                if (isExtraItem)
                {
                    st = isCollocation ? _extraItemCollocationStyle : _extraItemNormalStyle;
                }
                else
                {
                    st = isCollocation ? _collocationItemStyle : null;
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
