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
using Pronunciation.Trainer.ValueConverters;

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
        private readonly IgnoreEventsRegion _ignoreEvents = new IgnoreEventsRegion();

        private ExecuteActionCommand _commandBack;
        private ExecuteActionCommand _commandForward;
        private ExecuteActionCommand _commandClearText;
        private ExecuteActionCommand _commandPrevious;
        private ExecuteActionCommand _commandNext;
        private ExecuteActionCommand _commandFavorites;
        private ExecuteActionCommand _commandSyncPage;

        private const int MaxNumberOfSuggestions = 100;
        private const int VisibleNumberOfSuggestions = 30;

        public DictionaryContainer()
        {
            InitializeComponent();
        }

        private bool IsFilterActive
        {
            get { return !ReferenceEquals(_searchIndex, _mainIndex); }
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            _mainIndex = new DictionaryIndex();
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
            _commandPrevious = new ExecuteActionCommand(GoPreviousItem, false);
            _commandNext = new ExecuteActionCommand(GoNextItem, false);
            _commandFavorites = new ExecuteActionCommand(SetFavorite, false);
            _commandSyncPage = new ExecuteActionCommand(SynchronizePage, false);

            this.InputBindings.Add(new KeyBinding(_commandBack, KeyGestures.NavigateBack));
            this.InputBindings.Add(new KeyBinding(_commandForward, KeyGestures.NavigateForward));
            this.InputBindings.Add(new KeyBinding(_commandClearText, KeyGestures.ClearText));
            this.InputBindings.Add(new KeyBinding(_commandPrevious, KeyGestures.PreviousWord));
            this.InputBindings.Add(new KeyBinding(_commandNext, KeyGestures.NextWord));
            this.InputBindings.Add(new KeyBinding(_commandFavorites, KeyGestures.Favorites));
            this.InputBindings.Add(new KeyBinding(_commandSyncPage, KeyGestures.SyncWord));

            btnBack.Command = _commandBack;
            btnForward.Command = _commandForward;
            btnClearText.Command = _commandClearText;
            btnPrevious.Command = _commandPrevious;
            btnNext.Command = _commandNext;
            btnFavorites.Command = _commandFavorites;
            btnSyncPage.Command = _commandSyncPage;

            cboRanks.ItemsSource = GetWordLists();
            using (var region = _ignoreEvents.Start())
            {
                cboRanks.SelectedIndex = 0;
            }

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

            lstSuggestions.AttachItemsSource(new[] { GetHintSuggestion() });
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
                btnSyncPage.ToolTip = string.Format(btnSyncPage.ToolTip.ToString(), KeyGestures.SyncWord.DisplayString);
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
                }

                _commandFavorites.UpdateState(_currentPage.WordIndex != null);
                _commandSyncPage.UpdateState(_currentPage.WordIndex != null);

                RefreshAudioContext(sourceIndex ?? _currentPage.WordIndex);
                RefreshHistoryNavigationState();
            }
            catch (Exception ex)
            {
                _commandFavorites.UpdateState(false);

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

        private void GoPreviousItem()
        {
            var entry = lstSuggestions.SelectPreviousItem(true, true) as IndexEntry;
            if (entry != null)
            {
                NavigateWord(entry, NavigationSource.ListNavigation);
            }
        }

        private void GoNextItem()
        {
            var entry = lstSuggestions.SelectNextItem(true, true) as IndexEntry;
            if (entry != null)
            {
                NavigateWord(entry, NavigationSource.ListNavigation);
            }
        }

        private void SetFavorite()
        {
            bool isAdd = (btnFavorites.IsChecked == true);
        }

        private void ClearText()
        {
            txtSearch.Text = null;
            txtSearch.Focus();
        }

        private void SynchronizePage()
        {
            if (_currentPage == null || _currentPage.WordIndex == null)
                return;

            lstSuggestions.SelectItem(_currentPage.WordIndex, true, true);
        }

        private void cboRanks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_ignoreEvents.IsActive)
                return;

            var selectedItem = cboRanks.SelectedItem as KeyTextPair<int>;
            if (selectedItem == null)
                return;

            if (selectedItem.Key == 0)
            {
                _searchIndex = _mainIndex;
                LoadSuggestions(lstSuggestions, txtSearch.Text);
                if (lstSuggestions.Items.Count <= 0 && string.IsNullOrEmpty(txtSearch.Text))
                {
                    lstSuggestions.AttachItemsSource(new[] { GetHintSuggestion() });
                }
            }
            else
            {
                _searchIndex = new DictionaryIndex();
                _searchIndex.Build(_mainIndex.Entries.Where(x => x.UsageRank == selectedItem.Key), false);

                LoadSuggestions(lstSuggestions, txtSearch.Text);
                lstSuggestions.SelectFirstItem(false, true);
            }
        }

        private void cboCategories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_ignoreEvents.IsActive)
                return;

            LoadSuggestions(lstSuggestions, txtSearch.Text);
            if (lstSuggestions.Items.Count <= 0 && string.IsNullOrEmpty(txtSearch.Text) && !IsFilterActive)
            {
                lstSuggestions.AttachItemsSource(new[] { GetHintSuggestion() });
            }
            else
            {
                lstSuggestions.SelectFirstItem(false, true);
            }
        }

        private void LoadSuggestions(SuggestionsList lst, string searchText)
        {
            if (IsFilterActive)
            {
                if (string.IsNullOrEmpty(searchText))
                {
                    lstSuggestions.AttachItemsSource(_searchIndex.Entries);
                }
                else
                {
                    List<IndexEntry> extraEntries = null;
                    List<IndexEntry> filterEntries = _searchIndex.FindEntriesByText(searchText, false, -1);

                    // Search items in the main index
                    if (filterEntries == null || filterEntries.Count < VisibleNumberOfSuggestions)
                    {
                        extraEntries = _mainIndex.FindEntriesByText(searchText, false, MaxNumberOfSuggestions);
                        if (extraEntries != null)
                        {
                            if (filterEntries != null && filterEntries.Count > 0)
                            {
                                extraEntries = extraEntries.Where(x => !filterEntries.Contains(x)).ToList();
                            }
                            if (extraEntries.Count > 0)
                            {
                                if (extraEntries.Count == MaxNumberOfSuggestions)
                                {
                                    extraEntries.Add(new IndexEntryImitation("..."));
                                }
                                extraEntries.Insert(0, new IndexEntryImitation("*** Out of the filter ***"));
                            }
                        }
                    }

                    lstSuggestions.AttachItemsSource(filterEntries, extraEntries);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(searchText))
                {
                    lstSuggestions.AttachItemsSource<IndexEntry>(null);
                }
                else
                {
                    List<IndexEntry> filterEntries = _searchIndex.FindEntriesByText(searchText, false, MaxNumberOfSuggestions);
                    if (filterEntries != null && filterEntries.Count == MaxNumberOfSuggestions)
                    {
                        filterEntries.Add(new IndexEntryImitation("..."));
                    }

                    lstSuggestions.AttachItemsSource(filterEntries);
                }
            } 
        }

        private IndexEntryImitation GetHintSuggestion()
        {
            return new IndexEntryImitation("Start typing...");
        }

        private void txtSearch_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                lstSuggestions.SelectFirstItem(true, false);
            }
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            // Don't use KeyUp because it's also fired when we hit Enter in a MessageBox dialog
            if (e.Key == Key.Return)
            {
                e.Handled = true;
                ActivateFirstSuggestion();
            }
        }
        private void cboRanks_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                e.Handled = true;
                ActivateFirstSuggestion();
            }
        }
        private void cboCategories_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                e.Handled = true;
                ActivateFirstSuggestion();
            }
        }

        private void lstSuggestions_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            NavigateSelectedItem(lstSuggestions, NavigationSource.SuggestionsList);
        }

        private void lstSuggestions_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                NavigateSelectedItem(lstSuggestions, NavigationSource.SuggestionsList);
            }
        }

        private void lstSuggestions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _commandPrevious.UpdateState(lstSuggestions.CanSelectPrevious);
            _commandNext.UpdateState(lstSuggestions.CanSelectNext);
        }

        private void lstRecentWords_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            NavigateSelectedItem(lstRecentWords, NavigationSource.HistoryList);
        }

        private void lstRecentWords_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                NavigateSelectedItem(lstRecentWords, NavigationSource.HistoryList);
            }
        }

        private void ActivateFirstSuggestion()
        {
            var firstItem = lstSuggestions.SelectFirstItem(true, true);
            if (firstItem != null)
            {
                NavigateSelectedItem(lstSuggestions, NavigationSource.Search);
            }
            else
            {
                if (!string.IsNullOrEmpty(txtSearch.Text))
                {
                    MessageBox.Show(string.Format("Dictionary article for word '{0}' doesn't exist!", txtSearch.Text),
                        "Search result", MessageBoxButton.OK);
                }
                // Return focus back to textbox to immediately correct the error  
                txtSearch.Focus();                        
            }
        }

        private void NavigateSelectedItem(ListBox listBox, NavigationSource source)
        {
            var selectedItem = listBox.SelectedItem as IndexEntry;
            if (selectedItem == null || (selectedItem is IndexEntryImitation))
                return;

            var article = _currentPage == null ? null : (_currentPage.Page as ArticlePage);
            if (article != null && article.ArticleKey == selectedItem.ArticleKey)
            {
                // The same page - just play the sound without reloading the page
                RefreshAudioContext(selectedItem);
            }
            else
            {
                NavigateWord(selectedItem, source);
            }
        }

        private void NavigateWord(IndexEntry sourceIndex, NavigationSource sourceAction)
        {
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
                new KeyTextPair<int>(0, "none"),
                new KeyTextPair<int>(1000, "Top 1000"),
                new KeyTextPair<int>(2000, "Top 1000-2000"),
                new KeyTextPair<int>(3000, "Top 2000-3000"),
                new KeyTextPair<int>(5000, "Top 3000-5000"),
                new KeyTextPair<int>(7500, "Top 5000-7500")
            };
        }
    }

    // We can't make it inner class because we won't be able to reference it as static resource
    public class FavoritesTooltipArgsProvider : IStateToStringArgsProvider
    {
        public object[] GetTrueStringFormatArgs()
        {
            return new object[] { KeyGestures.Favorites.DisplayString };
        }

        public object[] GetFalseStringFormatArgs()
        {
            return new object[] { KeyGestures.Favorites.DisplayString };
        }
    }
}
