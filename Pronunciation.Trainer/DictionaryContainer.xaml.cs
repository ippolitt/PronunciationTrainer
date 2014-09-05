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
using Pronunciation.Trainer.Database;
using System.ComponentModel;
using Pronunciation.Trainer.Views;
using System.Media;

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
            public string WordName;
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
        private CategoryManager _categoryManager;
        private WordCategoryStateTracker _categoryTracker;
        private SessionStatisticsCollector _statsCollector;
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
        private const string StatisticsTemplate = "Session statistics: viewed {0} pages, recorded {1} audios";

        public DictionaryContainer()
        {
            InitializeComponent();
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            _mainIndex = new DictionaryIndex();
            _searchIndex = _mainIndex;

            _categoryManager = new CategoryManager(ProcessWordCategoriesChanged);
            _categoryTracker = new WordCategoryStateTracker(_categoryManager);
            _statsCollector = new SessionStatisticsCollector();
            _statsCollector.SessionStatisticsChanged += StatsCollector_SessionStatisticsChanged;

            //_dictionaryProvider = new LPDFileSystemProvider(AppSettings.Instance.Folders.Dictionary, CallScriptMethod);
            _dictionaryProvider = new LPDDatabaseProvider(AppSettings.Instance.Folders.Dictionary, 
                AppSettings.Instance.Folders.Database, AppSettings.Instance.Connections.Trainer);

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
            _commandSyncPage = new ExecuteActionCommand(SynchronizeSuggestions, false);

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
            btnSyncPage.Command = _commandSyncPage;
            btnFavorites.Command = _commandFavorites;

            btnFavorites.StateOnTooltipArgs = new object[] { KeyGestures.Favorites.DisplayString };
            btnFavorites.StateOffTooltipArgs = btnFavorites.StateOnTooltipArgs;
            btnFavorites.RefreshTooltip();
            lblSessionStats.Text = string.Format(StatisticsTemplate, 0, 0);

            using (var region = _ignoreEvents.Start())
            {
                PopulateRanks();
                cboRanks.SelectedIndex = 0;

                PopulateCategories();
                cboCategories.SelectedIndex = 0;
                categoriesDataGrid.IsEnabled = false;
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
                _mainIndex.Build(_dictionaryProvider.GetWordsIndex(AppSettings.Instance.DisplayLPDDataOnly), true);
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

        private void StatsCollector_SessionStatisticsChanged(int viewevPagesCount, int recordedWordsCount)
        {
            lblSessionStats.Text = string.Format(StatisticsTemplate, viewevPagesCount, recordedWordsCount);
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
                    string pageKey = ((ArticlePage)_currentPage.Page).ArticleKey;
                    _statsCollector.RegisterViewedPage(pageKey);

                    if (sourceIndex != null && !sourceIndex.IsCollocation)
                    {
                        _currentPage.WordIndex = sourceIndex;
                    }
                    else
                    {
                        _currentPage.WordIndex = _mainIndex.GetWordByPageKey(pageKey);
                    }

                    if (_currentPage.WordIndex != null)
                    {
                        _currentPage.WordName = _currentPage.WordIndex.EntryText;
                        if (sourceAction == NavigationSource.HistoryNavigation)
                        {
                            // Synchronize history list with the current word if we navigated from history
                            lstRecentWords.SelectItem(_currentPage.WordIndex, true, true);
                        }
                        else
                        {
                            // We register in history only words, not collocations
                            RegisterRecentWord(_currentPage.WordIndex);
                        }
                    }
                }

                _commandSyncPage.UpdateState(_currentPage.WordIndex != null);

                RefreshAudioContext(sourceIndex ?? _currentPage.WordIndex);
                RefreshHistoryNavigationState();
                RefreshCategoriesState(_currentPage.WordName);
            }
            catch (Exception ex)
            {
                RefreshCategoriesState(null);

                // For some reason errors in this event are not caught by the handlers in App.cs
                MessageHelper.ShowError(ex);
            }
        }

        private void AudioPanel_RecordingCompleted(string recordedFilePath, bool isTemporaryFile)
        {
            _audioContext.RegisterRecordedAudio(recordedFilePath, DateTime.Now);
            _statsCollector.RegisterRecordedWord(_audioContext.CurrentSoundName);
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

        private void RefreshCategoriesState(string wordName)
        {
            if (string.IsNullOrEmpty(wordName))
            {
                _categoryTracker.ResetWord();

                _commandFavorites.UpdateState(false);
                categoriesDataGrid.IsEnabled = false;
            }
            else
            {
                WordCategoryInfo info = _categoryManager.GetWordCategories(_currentPage.WordName);
                _categoryTracker.RegisterWord(_currentPage.WordName, info == null ? null : info.Categories);

                _commandFavorites.UpdateState(true);
                categoriesDataGrid.IsEnabled = true;
                btnFavorites.IsStateOn = info != null && info.IsInFavorites;
            }
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
            if (_currentPage == null || string.IsNullOrEmpty(_currentPage.WordName))
                return;

            if (btnFavorites.IsStateOn)
            {
                _categoryManager.RemoveFromFavorites(_currentPage.WordName);
                btnFavorites.IsStateOn = false;
            }
            else
            {
                _categoryManager.AddToFavorites(_currentPage.WordName);
                btnFavorites.IsStateOn = true;
            }
        }

        private void ClearText()
        {
            txtSearch.Text = null;
            txtSearch.Focus();
        }

        private void SynchronizeSuggestions()
        {
            if (_currentPage == null || _currentPage.WordIndex == null)
                return;

            if (!lstSuggestions.SelectItem(_currentPage.WordIndex, true, true))
            {
                SystemSounds.Beep.Play();
            }
        }

        private void cboRanks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_ignoreEvents.IsActive)
                return;

            ActivateFilter();
            RefreshSuggestions(true);
        }

        private void cboCategories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_ignoreEvents.IsActive)
                return;

            ActivateFilter();
            RefreshSuggestions(true);
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_ignoreEvents.IsActive)
                return;

            bool hasSuggestions = RefreshSuggestions(true);
            if (!hasSuggestions && ControlsHelper.HasTextBecomeLonger(e))
            {
                SystemSounds.Beep.Play();
            }
        }

        private void ProcessWordCategoriesChanged(string wordName, Guid[] addedCategoryIds, Guid[] removedCategoryIds)
        {
            var category = cboCategories.SelectedItem as DictionaryCategoryListItem;
            if (category == null || category.IsServiceItem)
                return;

            // If current category matches one of the modified we should refresh suggestions list
            if ((addedCategoryIds != null && addedCategoryIds.Contains(category.CategoryId))
                || (removedCategoryIds != null && removedCategoryIds.Contains(category.CategoryId)))
            {
                int initialPosition = lstSuggestions.SelectedIndex;
                bool restoreFocus = lstSuggestions.IsKeyboardFocusWithin;
                
                ActivateFilter();
                bool hasSuggestions = RefreshSuggestions(false);
                if (hasSuggestions)
                {
                    if (initialPosition <= 0)
                    {
                        lstSuggestions.SelectFirstItem(restoreFocus, true);
                    }
                    else
                    {
                        // Trying to restore list's position
                        lstSuggestions.SelectClosestItem(initialPosition, restoreFocus, true);
                    }
                }
            }
        }

        private void ActivateFilter()
        {
            var rank = cboRanks.SelectedItem as UsageRankListItem;
            bool isRank = rank != null && !rank.IsServiceItem;

            var category = cboCategories.SelectedItem as DictionaryCategoryListItem;
            bool isCategory = category != null && !category.IsServiceItem;

            if (!isRank && !isCategory)
            {
                _searchIndex = _mainIndex;
            }
            else
            {
                var query = isRank ? _mainIndex.Entries.Where(x => x.UsageRank == rank.Rank) : _mainIndex.Entries;
                if (isCategory)
                {
                    HashSet<string> categoryWords = _categoryManager.GetCategoryWords(category.CategoryId);
                    if (categoryWords != null && categoryWords.Count > 0)
                    {
                        query = query.Where(x => categoryWords.Contains(x.EntryText));
                    }
                    else
                    {
                        // It means there are no words in the category
                        query = new IndexEntry[0];
                    }
                }

                _searchIndex = new DictionaryIndex();
                _searchIndex.Build(query, false);
            }
        }

        private bool RefreshSuggestions(bool selectFirstItem)
        {
            string searchText = txtSearch.Text;
            bool isFilterActive = !ReferenceEquals(_searchIndex, _mainIndex);
            if (isFilterActive)
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

            bool hasSuggestions = lstSuggestions.Items.Count > 0;
            if (hasSuggestions)
            {
                if (selectFirstItem)
                {
                    lstSuggestions.SelectFirstItem(false, true);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(searchText))
                {
                    if (!isFilterActive)
                    {
                        lstSuggestions.AttachItemsSource(new[] { GetHintSuggestion() });
                    }
                }
                else
                {
                    lstSuggestions.AttachItemsSource(new[] { GetNoMatchSuggestion() });
                }                
            }

            return hasSuggestions;
        }

        private IndexEntryImitation GetHintSuggestion()
        {
            return new IndexEntryImitation("Start typing...");
        }

        private IndexEntryImitation GetNoMatchSuggestion()
        {
            return new IndexEntryImitation("No matching results");
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

        private void DataGridCell_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Enables editing on single click
            var cell = sender as DataGridCell;
            if (!cell.IsEditing)
            {
                if (!cell.IsFocused)
                {
                    cell.Focus();
                }
                if (!cell.IsSelected)
                {
                    cell.IsSelected = true;
                }
            }
        }

        private void btnEditCategories_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new EditCategories();
            dialog.Owner = ControlsHelper.GetWindow(this);
            dialog.ShowDialog();
            if (dialog.DialogResult == true)
            {
                var selectedCategory = cboCategories.SelectedItem as DictionaryCategoryListItem;
                bool isSelectionRestored = false;
                using (var region = _ignoreEvents.Start())
                {
                    PopulateCategories();
                    if (selectedCategory != null)
                    {
                        var matchedCategory = cboCategories.Items.OfType<DictionaryCategoryListItem>()
                            .FirstOrDefault(x => x.CategoryId == selectedCategory.CategoryId);
                        if (matchedCategory != null)
                        {
                            cboCategories.SelectedItem = matchedCategory;
                            isSelectionRestored = true;
                        }
                    }
                }

                // We do it outside of the ignoreEvents section to refresh the filter
                if (!isSelectionRestored)
                {
                    cboCategories.SelectedIndex = 0;
                }
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
                    MessageHelper.ShowInfo(
                        string.Format("Dictionary article for word '{0}' doesn't exist!", txtSearch.Text),
                        "Search result");
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

        private void PopulateRanks()
        {
            cboRanks.ItemsSource = new List<UsageRankListItem> { 
                new UsageRankListItem("none"),
                new UsageRankListItem(1000, "Top 1000"),
                new UsageRankListItem(2000, "Top 1000-2000"),
                new UsageRankListItem(3000, "Top 2000-3000"),
                new UsageRankListItem(5000, "Top 3000-5000"),
                new UsageRankListItem(7500, "Top 5000-7500")
            };
        }

        private void PopulateCategories()
        {
            var categories = _categoryManager.GetAllCategories().OrderBy(x => x).ToList();
            
            _categoryTracker.SynchronizeCategories(categories);
            categoriesDataGrid.ItemsSource = _categoryTracker.GetCategories();

            categories.Insert(0, new DictionaryCategoryListItem { DisplayName = "none", IsServiceItem = true });
            cboCategories.ItemsSource = categories;
        }
    }
}
