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
using Pronunciation.Core.Providers.Dictionary;
using Pronunciation.Trainer.AudioContexts;
using Pronunciation.Core.Contexts;
using Pronunciation.Core;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections;
using System.Diagnostics;
using Pronunciation.Trainer.Commands;
using Pronunciation.Core.Actions;
using Pronunciation.Core.Providers.Recording;
using Pronunciation.Core.Providers.Recording.HistoryPolicies;
using Pronunciation.Trainer.Utility;
using Pronunciation.Trainer.Recording;
using Pronunciation.Trainer.Dictionary;
using Pronunciation.Trainer.Controls;

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for DictionaryContainer.xaml
    /// </summary>
    public partial class DictionaryContainer : UserControlExt, ISupportsKeyboardFocus
    {
        private enum NavigationSource
        {
            Unknown,
            ListNavigation,
            HistoryNavigation,
            SuggestionsList,
            HistoryList,
            Search,
            PageHyperlink
        }

        private class LoadingPageInfo
        {
            public IndexEntry SourceIndex;
            public NavigationSource SourceAction;
            public ArticlePage TargetPage;
        }

        private class CurrentPageInfo
        {
            public PageInfo Page;
            public IndexEntry WordIndex;
        }

        private IDictionaryProvider _dictionaryProvider;
        private DictionaryAudioContext _audioContext;
        private DictionaryContainerScriptingProxy _scriptingProxy;
        private NavigationHistory<ArticlePage> _history;
        private LoadingPageInfo _loadingPage;
        private CurrentPageInfo _currentPage;
        private DictionaryIndex _mainIndex;
        private DictionaryIndex _searchIndex;
        private IndexPositionTracker _positionTracker;
        private bool _ignoreTextChanged;

        private ExecuteActionCommand _commandBack;
        private ExecuteActionCommand _commandForward;
        private ExecuteActionCommand _commandClearText;
        private ExecuteActionCommand _commandPrevious;
        private ExecuteActionCommand _commandNext;

        private const int MaxNumberOfSuggestions = 100;
        private const string ServiceArticleKey = "@";

        public DictionaryContainer()
        {
            InitializeComponent();
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            _mainIndex = new DictionaryIndex(0);
            _searchIndex = _mainIndex;

            //_dictionaryProvider = new LPDFileSystemProvider(AppSettings.Instance.Folders.DictionaryFile, CallScriptMethod);
            _dictionaryProvider = new LPDDatabaseProvider(AppSettings.Instance.Folders.DictionaryDB, AppSettings.Instance.Connections.LPD);

            _audioContext = new DictionaryAudioContext(_dictionaryProvider, _mainIndex,
                AppSettings.Instance.Recorders.LPD, new AppSettingsBasedRecordingPolicy());
            audioPanel.AttachContext(_audioContext);
            audioPanel.RecordingCompleted += AudioPanel_RecordingCompleted;

            _history = new NavigationHistory<ArticlePage>();
            _scriptingProxy = new DictionaryContainerScriptingProxy(
                (x,y) => _audioContext.PlayScriptAudio(x, y),
                (x) => NavigateWordFromHyperlink(x));
            browser.ObjectForScripting = _scriptingProxy;

            borderSearch.BorderBrush = txtSearch.BorderBrush;
            borderSearch.BorderThickness = new Thickness(1);

            _commandBack = new ExecuteActionCommand(GoBack, false);
            _commandForward = new ExecuteActionCommand(GoForward, false);
            _commandClearText = new ExecuteActionCommand(ClearText, true);
            _commandPrevious = new ExecuteActionCommand(GoPreviousWord, false);
            _commandNext = new ExecuteActionCommand(GoNextWord, false);

            this.InputBindings.Add(new KeyBinding(_commandBack, KeyGestures.NavigateBack));
            this.InputBindings.Add(new KeyBinding(_commandForward, KeyGestures.NavigateForward));
            this.InputBindings.Add(new KeyBinding(_commandClearText, KeyGestures.ClearText));
            this.InputBindings.Add(new KeyBinding(_commandPrevious, KeyGestures.PreviousWord));
            this.InputBindings.Add(new KeyBinding(_commandNext, KeyGestures.NextWord));

            btnBack.Command = _commandBack;
            btnForward.Command = _commandForward;
            btnClearText.Command = _commandClearText;
            btnPrevious.Command = _commandPrevious;
            btnNext.Command = _commandNext;

            cboWordLists.ItemsSource = GetWordLists();
            cboWordLists.SelectedIndex = 0;

            SplashScreen splash = null;
            if (!_dictionaryProvider.IsWordsIndexCached)
            {
                splash = new SplashScreen("Resources/BuildingIndex.png");
                splash.Show(false, true);
            }

            bool isSuccess = false;
            try
            {
                _mainIndex.Build(_dictionaryProvider.GetWordsIndex(), true);
                isSuccess = true;
            }
            finally
            {
                if (splash != null)
                {
                    splash.Close(TimeSpan.FromSeconds(isSuccess ? 1 : 0));
                }
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtSearch.Text) && !txtSearch.IsKeyboardFocusWithin)
            {
                txtSearch.Focus();
            }
            else
            {
                CaptureKeyboardFocus();
            }
        }

        public void NavigateWordFromHyperlink(string wordName)
        {
            if (string.IsNullOrEmpty(wordName))
                return;

            var wordIndex = _mainIndex.GetWordByName(wordName);
            if (wordIndex == null)
                return;

            NavigateWord(wordIndex, NavigationSource.PageHyperlink);
        }

        protected override void OnVisualTreeBuilt(bool isFirstBuild)
        {
            base.OnVisualTreeBuilt(isFirstBuild);

            if (isFirstBuild)
            {
                btnBack.ToolTip = string.Format(btnBack.ToolTip.ToString(), KeyGestures.NavigateBack.DisplayString);
                btnForward.ToolTip = string.Format(btnForward.ToolTip.ToString(), KeyGestures.NavigateForward.DisplayString);
                btnClearText.ToolTip = string.Format(btnClearText.ToolTip.ToString(), KeyGestures.ClearText.DisplayString);
                btnPrevious.ToolTip = string.Format(btnPrevious.ToolTip.ToString(), KeyGestures.PreviousWord.DisplayString);
                btnNext.ToolTip = string.Format(btnNext.ToolTip.ToString(), KeyGestures.NextWord.DisplayString);
            }
        }

        public void CaptureKeyboardFocus()
        {
            lstSuggestions.FocusSelectedItem();
        }

        private void browser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            try
            {
                _currentPage = new CurrentPageInfo();
                IndexEntry sourceIndex = null;
                NavigationSource sourceAction = NavigationSource.Unknown;
                if (_loadingPage == null)
                {
                    // It means that the page has been loaded by clicking on a hyperlink inside a previous page
                    _currentPage.Page = (e.Uri == null ? null : _dictionaryProvider.PrepareGenericPage(e.Uri));
                }
                else
                {
                    _currentPage.Page = _loadingPage.TargetPage;
                    sourceIndex = _loadingPage.SourceIndex;
                    sourceAction = _loadingPage.SourceAction;
                    _loadingPage = null;
                }

                IndexEntry activeIndex = null;
                if (_currentPage.Page is ArticlePage)
                {
                    if (sourceIndex != null && !sourceIndex.IsCollocation)
                    {
                        _currentPage.WordIndex = sourceIndex;
                    }
                    else
                    {
                        _currentPage.WordIndex = _mainIndex.GetWordByPageKey(((ArticlePage)_currentPage.Page).ArticleKey);
                    }

                    // We register only recent words, not collocations
                    if (_currentPage.WordIndex != null)
                    {
                        RegisterRecentWord(_currentPage.WordIndex);
                    }

                    activeIndex = (sourceIndex ?? _currentPage.WordIndex);
                    if (activeIndex != null && _positionTracker != null)
                    {
                        bool isRewinded = _positionTracker.RewindToEntry(activeIndex);
                        if (isRewinded)
                        {
                            SynchronizeSuggestions(activeIndex);
                        }
                    }
                }

                RefreshAudioContext(activeIndex);
                RefreshListNavigationState();
                RefreshHistoryNavigationState();
            }
            catch (Exception ex)
            {
                // For some reason errors in this event are not caught by the handlers in App.cs
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AudioPanel_RecordingCompleted(string recordedFilePath, bool isTemporaryFile)
        {
            _audioContext.RegisterRecordedAudio(recordedFilePath, DateTime.Now);
        }

        private void RefreshAudioContext(IndexEntry index)
        {
            _audioContext.RefreshContext(index,
                AppSettings.Instance.StartupMode == StartupPlayMode.British,
                AppSettings.Instance.StartupMode != StartupPlayMode.None);
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
            var existingItem = lstRecentWords.Items.Cast<IndexEntry>().FirstOrDefault(x => x == entry);
            if (existingItem == null)
            {
                lstRecentWords.Items.Insert(0, entry);
                lstRecentWords.SelectedIndex = 0;
            }
        }

        private void RefreshHistoryNavigationState()
        {
            _commandBack.UpdateState(_history.CanGoBack);
            _commandForward.UpdateState(_history.CanGoForward);
        }

        private void RefreshListNavigationState()
        {
            _commandPrevious.UpdateState(_positionTracker == null ? false : _positionTracker.CanGoPrevious);
            _commandNext.UpdateState(_positionTracker == null ? false : _positionTracker.CanGoNext);
        }

        private void GoBack()
        {
            if (_history.CanGoBack)
            {
                NavigatePage(_history.GoBack(), null, NavigationSource.HistoryNavigation, false);
            }
        }

        private void GoForward()
        {
            if (_history.CanGoForward)
            {
                NavigatePage(_history.GoForward(), null, NavigationSource.HistoryNavigation, false);
            }
        }

        private void GoPreviousWord()
        {
            if (_positionTracker == null || !_positionTracker.CanGoPrevious)
                return;

            IndexEntry entry = _positionTracker.GoPrevious();
            if (entry != null)
            {
                txtSearch.Text = null;
                NavigateWord(entry, NavigationSource.ListNavigation);
            }
        }

        private void GoNextWord()
        {
            if (_positionTracker == null || !_positionTracker.CanGoNext)
                return;

            IndexEntry entry = _positionTracker.GoNext();
            if (entry != null)
            {
                txtSearch.Text = null;
                NavigateWord(entry, NavigationSource.ListNavigation);
            }
        }

        private void ClearText()
        {
            txtSearch.Text = null;
        }

        private void cboWordLists_DropDownClosed(object sender, EventArgs e)
        {
            var selectedItem = cboWordLists.SelectedItem as KeyTextPair<int>;
            if (selectedItem == null || selectedItem.Key == _searchIndex.ID)
                return;

            if (selectedItem.Key == 0)
            {
                _searchIndex = _mainIndex;
                _positionTracker = null;
                lstSuggestions.ItemsSource = FindSuggestions(txtSearch.Text);
            }
            else
            {
                _searchIndex = new DictionaryIndex(selectedItem.Key);
                _searchIndex.Build(_mainIndex.Entries.Where(x => x.UsageRank == selectedItem.Key), false);
                _positionTracker = new IndexPositionTracker(_searchIndex);
                
                lstSuggestions.ItemsSource = _searchIndex.Entries;
                if (lstSuggestions.Items.Count > 0)
                {
                    IndexEntry currentWordIndex = _currentPage == null ? null : _currentPage.WordIndex;
                    bool isRewinded = false;
                    if (currentWordIndex != null)
                    {
                        isRewinded = _positionTracker.RewindToEntry(currentWordIndex);
                    }

                    if (isRewinded)
                    {
                        SynchronizeSuggestions(currentWordIndex);
                    }
                    else
                    {
                        lstSuggestions.SelectedIndex = 0;
                        lstSuggestions.ScrollIntoView(lstSuggestions.SelectedItem);
                    }
                }

                _ignoreTextChanged = true;
                txtSearch.Text = null;
                _ignoreTextChanged = false;
            }

            RefreshListNavigationState();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_ignoreTextChanged)
                return;

            lstSuggestions.ItemsSource = FindSuggestions(txtSearch.Text);
            SelectFirstSuggestion();
        }

        private IEnumerable<IndexEntry> FindSuggestions(string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
                return _searchIndex.ID == 0 ? null : _searchIndex.Entries;

            List<IndexEntry> entries = _searchIndex.FindEntriesByText(searchText, false, MaxNumberOfSuggestions);
            if (entries == null)
            {
                entries = new List<IndexEntry>();
            }

            if (entries.Count == MaxNumberOfSuggestions)
            {
                entries.Add(new IndexEntry(ServiceArticleKey, "...", false));
            }
            else if (entries.Count < MaxNumberOfSuggestions && !ReferenceEquals(_searchIndex, _mainIndex))
            {
                // Search items in the main index
                List<IndexEntry> mainEntries = _mainIndex.FindEntriesByText(searchText, false, MaxNumberOfSuggestions);
                if (mainEntries != null)
                {
                    var extraEntries = mainEntries.Where(x => !entries.Contains(x)).ToList();
                    if (extraEntries.Count > 0)
                    {
                        entries.Add(new IndexEntry(ServiceArticleKey, "*** Not in list ***", true));
                        entries.AddRange(extraEntries);
                        if (mainEntries.Count == MaxNumberOfSuggestions)
                        {
                            entries.Add(new IndexEntry(ServiceArticleKey, "...", false));
                        }
                    }
                }
            }

            return entries;
        }

        private void txtSearch_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                if (lstSuggestions.Items.Count > 0)
                {
                    SelectFirstSuggestion();
                    lstSuggestions.FocusSelectedItem();
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
                    MessageBox.Show(string.Format("Dictionary article for word '{0}' doesn't exist!", txtSearch.Text), 
                        "Search result", MessageBoxButton.OK);
                    txtSearch.Focus(); // return focus back to textbox to immediately correct the error
                }
                else
                {
                    SelectFirstSuggestion();
                    lstSuggestions.FocusSelectedItem();
                    NavigateWord((IndexEntry)lstSuggestions.SelectedItem, NavigationSource.Search);
                }
            }
        }

        private void lstSuggestions_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            NavigateSelectedItem(lstSuggestions, true);
        }

        private void lstSuggestions_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                NavigateSelectedItem(lstSuggestions, true);
            }
        }

        private void lstRecentWords_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            NavigateSelectedItem(lstRecentWords, false);
        }

        private void lstRecentWords_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                NavigateSelectedItem(lstRecentWords, false);
            }
        }

        private void SelectFirstSuggestion()
        {
            if (lstSuggestions.Items.Count <= 0)
                return;

            var firstItem = lstSuggestions.Items[0] as IndexEntry;
            if (firstItem != null && firstItem.ArticleKey == ServiceArticleKey)
            {
                // Avoid selection of the ServiceItem - select the next one if exists
                lstSuggestions.SelectedIndex = lstSuggestions.Items.Count > 1 ? 1 : 0;
            }
            else
            {
                lstSuggestions.SelectedIndex = 0;
            }
        }

        private void SynchronizeSuggestions(IndexEntry currentItem)
        {
            if (currentItem == null || lstSuggestions.Items.Count <= 0)
                return;

            if (!ReferenceEquals(lstSuggestions.SelectedItem, currentItem))
            {
                lstSuggestions.SelectedItem = currentItem;
            }

            if (ReferenceEquals(lstSuggestions.SelectedItem, currentItem))
            {
                lstSuggestions.ScrollIntoView(lstSuggestions.SelectedItem);
                lstSuggestions.FocusSelectedItem();
            }
        }

        private void NavigateSelectedItem(ListBox listBox, bool isSuggestionsList)
        {
            var selectedItem = listBox.SelectedItem as IndexEntry;
            if (selectedItem == null || selectedItem.ArticleKey == ServiceArticleKey)
                return;

            var article = _currentPage == null ? null : (_currentPage.Page as ArticlePage);
            if (article != null && article.ArticleKey == selectedItem.ArticleKey)
            {
                // The same page - just play the sound without reloading the page
                RefreshAudioContext(selectedItem);
                return;
            }

            NavigateWord(selectedItem, isSuggestionsList ? NavigationSource.SuggestionsList : NavigationSource.HistoryList);
        }

        private void NavigateWord(IndexEntry sourceIndex, NavigationSource sourceAction)
        {
            if (sourceIndex.ArticleKey == ServiceArticleKey)
                return;

            ArticlePage article = _dictionaryProvider.PrepareArticlePage(sourceIndex.ArticleKey);
            NavigatePage(article, sourceIndex, sourceAction, true);
        }

        private void NavigatePage(ArticlePage targetPage, IndexEntry sourceIndex, NavigationSource sourceAction, bool registerHistory)
        {
            if (registerHistory)
            {
                _history.RegisterPage(targetPage);
            }

            _loadingPage = new LoadingPageInfo { SourceIndex = sourceIndex, SourceAction = sourceAction, TargetPage = targetPage };
            if (targetPage.LoadByUrl)
            {
                browser.Navigate(PrepareNavigationUri(targetPage.PageUrl));
            }
            else
            {
                browser.NavigateToString(targetPage.PageHtml);
            }
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

        private string CallScriptMethod(string methodName, object[] methodArgs)
        {
            object scriptResult = browser.InvokeScript(methodName, methodArgs);
            return (scriptResult is DBNull ? null : (string)scriptResult);
        }

        private List<KeyTextPair<int>> GetWordLists()
        {
            return new List<KeyTextPair<int>> { 
                new KeyTextPair<int>(0, "All words"),
                new KeyTextPair<int>(1000, "Top 1000 words"),
                new KeyTextPair<int>(2000, "Top 1000-2000 words"),
                new KeyTextPair<int>(3000, "Top 2000-3000 words"),
                new KeyTextPair<int>(5000, "Top 3000-5000 words"),
                new KeyTextPair<int>(7500, "Top 5000-7500 words")
            };
        }
    }
}
