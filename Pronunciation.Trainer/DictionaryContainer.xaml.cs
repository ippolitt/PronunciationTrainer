using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
//using System.Windows.Shapes;
using Pronunciation.Core.Providers;
using Pronunciation.Trainer.AudioContexts;
using Pronunciation.Core.Contexts;
using Pronunciation.Core;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections;
using System.Diagnostics;

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for DictionaryContainer.xaml
    /// </summary>
    public partial class DictionaryContainer : UserControl
    {
        private class SearchTextComparer : IComparer<string>
        {
            private readonly string _searchText;

            public SearchTextComparer(string searchText)
            {
                _searchText = searchText;
            }

            public int Compare(string x, string y)
            {
                int result = 0;
                if (x == y || (x == null && y == null))
                {
                    result = 0;
                }
                else if (x == null)
                {
                    result = -1;
                }
                else if (y == null)
                {
                    result = 1;
                }
                else if (string.Equals(x, y, StringComparison.OrdinalIgnoreCase))
                {
                    // We rank case-sensitive match with the search text higher than case-insensitive one.
                    // So if search text is "A", then we display: "A, a" 
                    if (x.StartsWith(_searchText))
                    {
                        result = -1;
                    }
                    else if (y.StartsWith(_searchText))
                    {
                        result = 1;
                    }
                    else
                    {
                        result = x.CompareTo(y);
                    }
                }
                else
                {
                    result = x.CompareTo(y);
                }

                return result;
            }
        }

        private LPDProvider _provider;
        private DictionaryAudioContext _audioContext;
        private KeyTextPair<string>[] _words;

        private const int MaxNumberOfSuggestions = 50;
        private const string MoreSuggestionsKey = "...";

        public DictionaryContainer()
        {
            InitializeComponent();
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            _provider = new LPDProvider(AppSettings.Instance.BaseFolder);
            _audioContext = new DictionaryAudioContext(_provider, GetPageAudioData);

            _words = _provider.GetWords().OrderBy(x => x.Text).ToArray();

            var wordLists = _provider.GetWordLists();
            if (wordLists != null)
            {
                cboWordLists.ItemsSource = wordLists;
            }

            borderSearch.BorderBrush = txtSearch.BorderBrush;
            borderSearch.BorderThickness = new Thickness(1);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            audioPanel.AttachContext(_audioContext);
            browser.ObjectForScripting = _audioContext;

            SetupControlsState();
            if (!txtSearch.IsKeyboardFocusWithin)
            {
                txtSearch.Focus();
            }
        }

        private void browser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            PageInfo previousPage = _audioContext.ActivePage;
            _audioContext.RefreshContext(e.Uri, 
                AppSettings.Instance.StartupMode == StartupPlayMode.British,
                AppSettings.Instance.StartupMode != StartupPlayMode.None);

            if (_audioContext.ActivePage == null)
            {
                cboWordLists.SelectedItem = null;
            }
            else if (_audioContext.ActivePage.IsWord)
            {
                KeyTextPair<string> item = _words.FirstOrDefault(x => x.Key == _audioContext.ActivePage.PageName);
                if (item != null)
                {
                    RegisterRecentWord(item);
                }
                if (txtSearch.IsKeyboardFocusWithin)
                {
                    browser.Focus();
                }

                cboWordLists.SelectedItem = null;
            }

            SetupControlsState();
        }

        private void RegisterRecentWord(KeyTextPair<string> wordItem)
        {
            var currentIndex = lstRecentWords.SelectedIndex;
            var existingItem = lstRecentWords.Items.Cast<KeyTextPair<string>>().FirstOrDefault(x => x.Key == wordItem.Key);
            if (existingItem != null)
            {
                lstRecentWords.Items.Remove(existingItem);
            }

            lstRecentWords.Items.Insert(0, new KeyTextPair<string>(wordItem.Key, wordItem.Text));
            lstRecentWords.SelectedIndex = currentIndex >= 0 ? currentIndex : 0;
        }

        private void SetupControlsState()
        {
            btnBack.IsEnabled = browser.CanGoBack;
            btnForward.IsEnabled = browser.CanGoForward;
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (browser.CanGoBack)
            {
                browser.GoBack();
            }
        }

        private void btnForward_Click(object sender, RoutedEventArgs e)
        {
            if (browser.CanGoForward)
            {
                browser.GoForward();
            }
        }

        private void cboWordLists_DropDownClosed(object sender, EventArgs e)
        {
            if (cboWordLists.SelectedItem == null)
                return;

            NavigateList(((KeyTextPair<string>)cboWordLists.SelectedItem).Key);
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var items = FindWordsByText(txtSearch.Text, false, MaxNumberOfSuggestions);
            if (items != null && items.Count == MaxNumberOfSuggestions)
            {
                items.Add(new KeyTextPair<string>(MoreSuggestionsKey, "..."));
            }
            lstSuggestions.ItemsSource = items;
            if (lstSuggestions.Items.Count > 0)
            {
                lstSuggestions.SelectedIndex = 0;
            }
        }

        private void txtSearch_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                if (lstSuggestions.Items.Count > 0)
                {
                    lstSuggestions.Focus();
                    lstSuggestions.SelectedIndex = 0;
                }
            }
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            // Don't use KeyUp because it's also fired when we hit Enter in a MessageBox dialog
            if (e.Key == Key.Return)
            {
                e.Handled = true;
                if (lstSuggestions.Items.Count <= 0)
                {
                    MessageBox.Show(string.Format("Dictionary article for word '{0}' doesn't exist!", txtSearch.Text));  
                }
                else
                {
                    NavigateWord(((KeyTextPair<string>)lstSuggestions.Items[0]).Key);
                }
            }
        }

        private List<KeyTextPair<string>> FindWordsByText(string wordText, bool isExactMatch, int maxItems)
        {
            if (string.IsNullOrWhiteSpace(wordText))
                return null;

            string searchText = wordText.Trim();
            IEnumerable<KeyTextPair<string>> query;
            if (isExactMatch)
            {
                query = _words.Where(x => string.Equals(x.Text, searchText, StringComparison.OrdinalIgnoreCase));   
            }
            else
            {
                query = _words.Where(x => x.Text.StartsWith(searchText, StringComparison.OrdinalIgnoreCase));
            }

            if (maxItems >= 0)
            {
                query = query.Take(maxItems);
            }

            return query.OrderBy(x => x.Text, new SearchTextComparer(searchText)).ToList();
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (KeyboardMapper.IsRegularKey(e))
            {
                if (!txtSearch.IsKeyboardFocusWithin && !audioPanel.HasKeyboardFocus)
                {
                    txtSearch.Text = null;
                    txtSearch.Focus();
                }
            }
        }

        private void lstSuggestions_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            NavigateListboxItem(lstSuggestions);
        }

        private void lstSuggestions_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                NavigateListboxItem(lstSuggestions);
            }
        }

        private void lstRecentWords_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            NavigateListboxItem(lstRecentWords);
        }

        private void lstRecentWords_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                NavigateListboxItem(lstRecentWords);
            }
        }

        private void NavigateListboxItem(ListBox listBox)
        {
            var selectedItem = listBox.SelectedItem as KeyTextPair<string>;
            if (selectedItem == null || selectedItem.Key == MoreSuggestionsKey)
                return;

            if (_audioContext.ActivePage != null && _audioContext.ActivePage.IsWord 
                && _audioContext.ActivePage.PageName == selectedItem.Key)
                return;

            NavigateWord(selectedItem.Key);
        }

        private void NavigateWord(string word)
        {
            Uri pageUrl = _provider.BuildWordPath(word);
            browser.Navigate(PrepareNavigationUri(pageUrl));
        }

        private void NavigateList(string listName)
        {
            Uri pageUrl = _provider.BuildWordListPath(listName);
            browser.Navigate(PrepareNavigationUri(pageUrl));
        }

        private Uri PrepareNavigationUri(Uri pageUrl)
        {
            if (pageUrl.Scheme == Uri.UriSchemeFile && string.IsNullOrEmpty(pageUrl.Host))
            {
                // Transform local path "C:\Folder\...\file.html" to "file://127.0.0.1/C$/Folder/.../file.html"
                // otherwise browser displays "Allow blocked content" warning
                // TODO: will it work for non-admins (because of the "C$")?
                var disk = Path.GetPathRoot(pageUrl.LocalPath);
                if (!string.IsNullOrEmpty(disk))
                {
                    return new Uri(string.Format("file://127.0.0.1/{0}$/{1}",
                        disk.Substring(0, 1),
                        pageUrl.LocalPath.Remove(0, disk.Length)));
                }
            }

            return pageUrl;
        }

        private string GetPageAudioData(string methodName, object[] methodArgs)
        {
            object scriptResult = browser.InvokeScript(methodName, methodArgs);
            return (scriptResult is DBNull) ? null : (string)scriptResult;
        }
    }
}
