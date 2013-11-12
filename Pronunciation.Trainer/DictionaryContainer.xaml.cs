﻿using System;
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
using Pronunciation.Trainer.Commands;

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for DictionaryContainer.xaml
    /// </summary>
    public partial class DictionaryContainer : UserControlExt, ISupportsKeyboardFocus
    {
        private class TokenizedIndexEntry
        {
            public string Token;
            public int Rank;
            public IndexEntry Entry;
        }

        private LPDProvider _provider;
        private DictionaryAudioContext _audioContext;
        private IndexEntry[] _wordsIndex;
        private TokenizedIndexEntry[] _tokensIndex;
        private PageInfo _loadingPage;
        private PageInfo _currentPage;

        private ExecuteActionCommand _commandBack;
        private ExecuteActionCommand _commandForward;

        private const int MaxNumberOfSuggestions = 100;
        private const string MoreSuggestionsKey = "...";

        public DictionaryContainer()
        {
            InitializeComponent();
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            _provider = new LPDProvider(AppSettings.Instance.Folders.Dictionary, AppSettings.Instance.Folders.DictionaryRecordings);
            
            _audioContext = new DictionaryAudioContext(_provider, GetPageAudioData);
            audioPanel.AttachContext(_audioContext);
            browser.ObjectForScripting = _audioContext;

            BuildIndex(_provider.GetWords());

            var wordLists = _provider.GetWordLists();
            if (wordLists != null)
            {
                cboWordLists.ItemsSource = wordLists;
            }

            borderSearch.BorderBrush = txtSearch.BorderBrush;
            borderSearch.BorderThickness = new Thickness(1);

            _commandBack = new ExecuteActionCommand(GoBack, false);
            _commandForward = new ExecuteActionCommand(GoForward, false);

            btnBack.Command = _commandBack;
            btnForward.Command = _commandForward;

            this.InputBindings.Add(new KeyBinding(_commandBack, KeyGestures.NavigateBack));
            this.InputBindings.Add(new KeyBinding(_commandForward, KeyGestures.NavigateForward));
        }

        protected override void OnVisualTreeBuilt(bool isFirstBuild)
        {
            base.OnVisualTreeBuilt(isFirstBuild);

            if (isFirstBuild)
            {
                btnBack.ToolTip += KeyGestures.NavigateBack.GetTooltipString();
                btnForward.ToolTip += KeyGestures.NavigateForward.GetTooltipString();
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            SetupControlsState();
            if (string.IsNullOrEmpty(txtSearch.Text) && !txtSearch.IsKeyboardFocusWithin)
            {
                txtSearch.Focus();
            }
            else
            {
                CaptureKeyboardFocus();
            }
        }

        public void CaptureKeyboardFocus()
        {
            lstSuggestions.Focus();
        }

        private void browser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            _currentPage = _loadingPage;
            _loadingPage = null;

            // It means that the page has been loaded by clicking on a hyperlink inside a previous page
            if (_currentPage == null)
            {
                _currentPage = _provider.GetPageInfo(e.Uri);
                if (_currentPage != null && _currentPage.IsWord)
                {
                    _currentPage.Index = _wordsIndex.FirstOrDefault(x => x.Key == _currentPage.PageName && !x.IsCollocation);
                }
            }

            _audioContext.RefreshContext(_currentPage, 
                AppSettings.Instance.StartupMode == StartupPlayMode.British,
                AppSettings.Instance.StartupMode != StartupPlayMode.None);

            if (_currentPage == null)
            {
                cboWordLists.SelectedItem = null;
            }
            else if (_currentPage.IsWord)
            {
                if (_currentPage.Index != null)
                {
                    RegisterRecentWord(_currentPage.Index);
                }

                // Move keyboard focus to another control
                if (txtSearch.IsKeyboardFocusWithin)
                {
                    CaptureKeyboardFocus();
                }

                cboWordLists.SelectedItem = null;
            }

            SetupControlsState();
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (KeyboardMapper.IsRegularKey(e) && e.Key != Key.Space)
            {
                if (!txtSearch.IsKeyboardFocusWithin && !audioPanel.HasKeyboardFocus)
                {
                    txtSearch.Text = null;
                    txtSearch.Focus();
                }
            }
        }

        private void RegisterRecentWord(IndexEntry entry)
        {
            var currentIndex = lstRecentWords.SelectedIndex;
            var existingItem = lstRecentWords.Items.Cast<IndexEntry>().FirstOrDefault(x => x == entry);
            if (existingItem != null)
            {
                lstRecentWords.Items.Remove(existingItem);
            }

            lstRecentWords.Items.Insert(0, entry);
            lstRecentWords.SelectedIndex = currentIndex >= 0 ? currentIndex : 0;
        }

        private void SetupControlsState()
        {
            _commandBack.UpdateState(browser.CanGoBack);
            _commandForward.UpdateState(browser.CanGoForward);
        }

        private void GoBack()
        {
            if (browser.CanGoBack)
            {
                browser.GoBack();
            }
        }

        private void GoForward()
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
                items.Add(new IndexEntry(MoreSuggestionsKey, "...", false));
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
                    lstSuggestions.SelectedIndex = 0;
                    NavigateWord((IndexEntry)lstSuggestions.Items[0]);
                }
            }
        }

        private List<IndexEntry> FindWordsByText(string wordText, bool isExactMatch, int maxItems)
        {
            if (string.IsNullOrWhiteSpace(wordText))
                return null;

            string searchText = wordText.Trim();
            IEnumerable<IndexEntry> query;
            if (isExactMatch)
            {
                query = _wordsIndex.Where(x => string.Equals(x.Text, searchText, StringComparison.OrdinalIgnoreCase));   
            }
            else
            {
                query = _wordsIndex.Where(x => x.Text.StartsWith(searchText, StringComparison.OrdinalIgnoreCase));
            }
            if (maxItems >= 0)
            {
                query = query.Take(maxItems);
            }

            var entries = query.OrderBy(x => x.Text, new SearchTextComparer(searchText)).ToList();
            if (!isExactMatch && (entries.Count < maxItems || maxItems < 0))
            {
                // Add token based matches
                var tokensQuery =_tokensIndex.Where(x => x.Token.StartsWith(searchText, StringComparison.OrdinalIgnoreCase));
                if (maxItems >= 0)
                {
                    tokensQuery = tokensQuery.Take(maxItems - entries.Count);
                }
                entries.AddRange(tokensQuery.OrderBy(x => x.Rank).ThenBy(x => x.Entry.Text).Select(x => x.Entry).Distinct());
            }

            return entries;
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
            var selectedItem = listBox.SelectedItem as IndexEntry;
            if (selectedItem == null || selectedItem.Key == MoreSuggestionsKey)
                return;

            if (_currentPage != null && _currentPage.IsWord && _currentPage.PageName == selectedItem.Key)
            {
                if (_currentPage.Index == null || _currentPage.Index == selectedItem)
                    return;
            }

            NavigateWord(selectedItem);
        }

        private void NavigateWord(IndexEntry entry)
        {
            Uri pageUrl = _provider.BuildWordPath(entry.Key);
            _loadingPage = new PageInfo(true, entry.Key);
            _loadingPage.Index = entry;
            browser.Navigate(PrepareNavigationUri(pageUrl));
        }

        private void NavigateList(string listName)
        {
            Uri pageUrl = _provider.BuildWordListPath(listName);
            _loadingPage = new PageInfo(false, listName);
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

        // Test /s
        private void BuildIndex(IEnumerable<IndexEntry> entries)
        {
            // Build index for StartWith match
            _wordsIndex = entries.OrderBy(x => x.Text).ToArray();

            // Build index for token-based match
            var tokens = new List<TokenizedIndexEntry>();
            foreach (var entry in _wordsIndex)
            {
                int rank = 0;
                int position = 0;
                bool isTokenStart = false;
                foreach(char ch in entry.Text)
                {
                    if (ch == ' ' || ch == '-' || ch == ',' || ch == '/' || ch == '(' || ch == ')')
                    {
                        // As soon as we hit a separator the next character might be the beginning of a token
                        // It means that we'll skip first words in the collocations 
                        // (because this case is covered by the _wordsIndex array)
                        isTokenStart = true;
                    }
                    else
                    {
                        if (isTokenStart)
                        {
                            isTokenStart = false;

                            // Add the whole remaining string as a token to enable multi-words match: 
                            // e.g. match "lot car" in "parking lot car"
                            string token = entry.Text.Substring(position);

                            // Don't add 1-symbol tokens
                            if (token.Length > 1)
                            {
                                tokens.Add(new TokenizedIndexEntry
                                {
                                    Rank = rank,
                                    Token = token,
                                    Entry = entry
                                });
                            }

                            rank++;
                        }
                    }

                    position++;
                }
            }

            _tokensIndex = tokens.OrderBy(x => x.Rank).ThenBy(x => x.Entry.Text).ToArray();
        }

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

    }
}
