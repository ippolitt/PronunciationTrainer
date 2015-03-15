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
using System.Threading.Tasks;
using Pronunciation.Core.Utility;
using Pronunciation.Core.Providers.Categories;
using Pronunciation.Core.Database;

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
            public int? WordId
            {
                get { return WordIndex == null ? null : WordIndex.WordId; }
            }
        }

        private IDictionaryProvider _dictionaryProvider;
        private DictionaryAudioContext _audioContext;
        private DictionaryContainerScriptingProxy _scriptingProxy;
        private NavigationHistory<ArticlePage> _history;
        private LoadingPageInfo _loadingPage;
        private CurrentPageInfo _currentPage;
        private DictionaryIndex _mainIndex;
        private DictionaryIndex _searchIndex;
        private DictionaryInitializer _dictionaryLoader;
        private CategoryManager _categoryManager;
        private AutoListsManager _autoListsManager;
        private WordCategoryStateTracker _categoryTracker;
        private SessionStatisticsCollector _statsCollector;
        private readonly IgnoreEventsRegion _ignoreEvents = new IgnoreEventsRegion();
        private bool _isFirstPageLoad = true;
        private bool _isFirstCategoriesLoad = true;

        private ExecuteActionCommand _commandBack;
        private ExecuteActionCommand _commandForward;
        private ExecuteActionCommand _commandClearText;
        private ExecuteActionCommand _commandPrevious;
        private ExecuteActionCommand _commandNext;
        private ExecuteActionCommand _commandSyncPage;
        private ExecuteActionCommand _commandEditNotes;

        private const int MaxNumberOfSuggestions = 100;
        private const int VisibleNumberOfSuggestions = 30;
        private const string StatisticsTemplate = "Session statistics: viewed {0} pages, recorded {1} audios";
        private const string HighlighAudioMethodName = "extHiglightAudio";
        private const string RefreshNotesMethodName = "extRefreshNotes";
        private const string ResetNotesMethodName = "extResetNotes"; 

        public DictionaryContainer()
        {
            InitializeComponent();
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            _mainIndex = new DictionaryIndex();
            _searchIndex = _mainIndex;

            _categoryManager = new CategoryManager(AppSettings.Instance.Connections.Trainer, ProcessWordCategoryChanged);
            _categoryTracker = new WordCategoryStateTracker(_categoryManager);
            _statsCollector = new SessionStatisticsCollector();
            _statsCollector.SessionStatisticsChanged += StatsCollector_SessionStatisticsChanged;

            //_dictionaryProvider = new LPDFileSystemProvider(AppSettings.Instance.Folders.Dictionary, CallScriptMethod);
            _dictionaryProvider = new DatabaseProvider(AppSettings.Instance.Folders.Dictionary, 
                AppSettings.Instance.Folders.Database, AppSettings.Instance.Connections.Trainer);
            IndexEntry.ActiveProvider = _dictionaryProvider;
            _dictionaryLoader = new DictionaryInitializer(_mainIndex, _dictionaryProvider);
            _autoListsManager = new AutoListsManager(_dictionaryProvider);

            _audioContext = new DictionaryAudioContext(_dictionaryProvider, AppSettings.Instance.Recorders.Dictionary, 
                new AppSettingsBasedRecordingPolicy());
            audioPanel.AttachContext(_audioContext);
            audioPanel.RecordingCompleted += AudioPanel_RecordingCompleted;

            _history = new NavigationHistory<ArticlePage>();
            _scriptingProxy = new DictionaryContainerScriptingProxy(_audioContext.PlayScriptAudio, NavigateWordFromHyperlink);
            browser.ObjectForScripting = _scriptingProxy;

            borderSearch.BorderBrush = txtSearch.BorderBrush;
            borderSearch.BorderThickness = new Thickness(1);

            _commandBack = new ExecuteActionCommand(GoBack, false);
            _commandForward = new ExecuteActionCommand(GoForward, false);
            _commandClearText = new ExecuteActionCommand(ClearText, true);
            _commandPrevious = new ExecuteActionCommand(GoPreviousItem, false);
            _commandNext = new ExecuteActionCommand(GoNextItem, false);
            _commandSyncPage = new ExecuteActionCommand(SynchronizeSuggestions, false);
            _commandEditNotes = new ExecuteActionCommand(EditNotes, false);

            this.InputBindings.Add(new KeyBinding(_commandBack, KeyGestures.NavigateBack));
            this.InputBindings.Add(new KeyBinding(_commandForward, KeyGestures.NavigateForward));
            this.InputBindings.Add(new KeyBinding(_commandClearText, KeyGestures.ClearText));
            this.InputBindings.Add(new KeyBinding(_commandPrevious, KeyGestures.PreviousWord));
            this.InputBindings.Add(new KeyBinding(_commandNext, KeyGestures.NextWord));
            this.InputBindings.Add(new KeyBinding(_commandSyncPage, KeyGestures.SyncWord));
            this.InputBindings.Add(new KeyBinding(_commandEditNotes, KeyGestures.EditNotes));

            btnBack.Command = _commandBack;
            btnForward.Command = _commandForward;
            btnClearText.Command = _commandClearText;
            btnPrevious.Command = _commandPrevious;
            btnNext.Command = _commandNext;
            btnSyncPage.Command = _commandSyncPage;
            btnEditNotes.Command = _commandEditNotes;

            lblSessionStats.Text = string.Format(StatisticsTemplate, 0, 0);

            using (var region = _ignoreEvents.Start())
            {
                PopulateRanks();
                cboRanks.SelectedIndex = 0;

                Logger.Info("Loading categories...");
                PopulateCategories();
                Logger.Info("Categories loaded.");

                cboCategories.SelectedIndex = 0;
                categoriesDataGrid.IsEnabled = false;

                lstSuggestions.AttachItemsSource(new[] { GetHintSuggestion() });
            }

            _dictionaryLoader.InitializeAsync(new Task(Warmup));
        }

        private void Warmup()
        {
            Logger.Info("Warming up sounds store...");
            _dictionaryProvider.WarmUpSoundsStore();

            Logger.Info("Warming up entity framework...");
            EntityFrameworkHelper.WarmUpFramework();

            Logger.Info("Warmup completed.");
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

        private void StatsCollector_SessionStatisticsChanged(int viewevPagesCount, int recordedAudiosCount)
        {
            lblSessionStats.Text = string.Format(StatisticsTemplate, viewevPagesCount, recordedAudiosCount);
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

                    _currentPage.WordIndex = ((ArticlePage)_currentPage.Page).PageIndex;
                    if (_currentPage.WordIndex != null)
                    {
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
                RefreshHistoryNavigationState();

                if (_isFirstPageLoad)
                {
                    Logger.Info("Page load completed. Refreshing audio context...");
                }
                RefreshAudioContext(sourceIndex ?? _currentPage.WordIndex);

                if (_isFirstPageLoad)
                {
                    Logger.Info("Audio context refreshed. Refreshing notes state...");
                }
                RefreshNotesState(_currentPage.WordIndex);

                if (_isFirstPageLoad)
                {
                    Logger.Info("Notes state refreshed. Refreshing categories state...");
                }
                RefreshCategoriesState(_currentPage.WordId);

                if (_isFirstPageLoad)
                {
                    Logger.Info("Categories state refreshed.");
                }
            }
            catch (Exception ex)
            {
                RefreshCategoriesState(null);

                // For some reason errors in this event are not caught by the handlers in App.cs
                MessageHelper.ShowError(ex);
            }
            finally
            {
                _isFirstPageLoad = false;
            }
        }

        private void AudioPanel_RecordingCompleted(string recordedFilePath, bool isTemporaryFile)
        {
            _audioContext.RegisterRecordedAudio(recordedFilePath, DateTime.Now);
            _statsCollector.RegisterRecordedAudio(_audioContext.CurrentSoundKey);
        }

        private void RefreshAudioContext(IndexEntry index)
        {
            string activeSoundKey = _audioContext.RefreshContext(index,
                AppSettings.Instance.StartupMode == StartupPlayMode.British,
                AppSettings.Instance.StartupMode != StartupPlayMode.None);

            if (AppSettings.Instance.StartupMode != StartupPlayMode.None)
            {
                CallScriptMethod(HighlighAudioMethodName, new string[] { activeSoundKey });
            }
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

        private void RefreshCategoriesState(int? wordId)
        {
            if (wordId == null)
            {
                _categoryTracker.ResetWord();
                categoriesDataGrid.IsEnabled = false;
            }
            else
            {
                _categoryTracker.RegisterWord(wordId.Value);
                categoriesDataGrid.IsEnabled = true;
            }
        }

        private void RefreshNotesState(IndexEntry index)
        {
            if (index == null)
            {
                _commandEditNotes.UpdateState(false);
            }
            else
            {
                _commandEditNotes.UpdateState(true);

                DictionaryWordInfo word = index.Word;
                if (word.HasNotes)
                {
                    CallScriptMethod(RefreshNotesMethodName, new string[] 
                    { 
                        HtmlHelper.PrepareHtmlContent(word.FavoriteTranscription), 
                        HtmlHelper.PrepareHtmlContent(word.Notes, true) 
                    });
                }
                else
                {
                    CallScriptMethod(ResetNotesMethodName, null);
                }
            }
        }

        private void EditNotes()
        {
            if (_currentPage == null || _currentPage.WordId == null)
                throw new ArgumentNullException();

            var dialog = new WordNotes();
            dialog.WordId = _currentPage.WordId.Value;
            dialog.WordInfoUpdated += WordInfoUpdated;
            dialog.Owner = ControlsHelper.GetWindow(this);
            dialog.Show();
        }

        private void WordInfoUpdated(DictionaryWord wordDetails)
        {
            bool refreshNotesState = false;
            IndexEntry wordIndex;
            if (_currentPage != null && _currentPage.WordId == wordDetails.WordId)
            {
                wordIndex = _currentPage.WordIndex;
                refreshNotesState = true;
            }
            else
            {
                wordIndex = _mainIndex.GetWordById(wordDetails.WordId);
            }

            DictionaryWordInfo wordInfo = wordIndex.Word;
            bool hadNotes = wordInfo.HasNotes;
            wordInfo.FavoriteTranscription = wordDetails.FavoriteTranscription;
            wordInfo.Notes = wordDetails.Notes;
            wordInfo.HasNotes = wordDetails.HasNotes == true;

            if (refreshNotesState)
            {
                RefreshNotesState(wordIndex);
            }

            if (hadNotes != wordInfo.HasNotes)
            {
                ProcessWordCategoryChanged(wordDetails.WordId, AutoListsManager.WordsWithNotes, hadNotes);
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

            RefreshSuggestions(true, true);
        }

        private void cboCategories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_ignoreEvents.IsActive)
                return;

            if (_isFirstCategoriesLoad)
            {
                Logger.Info("Loading category words...");
            }

            RefreshSuggestions(true, true);

            if (_isFirstCategoriesLoad)
            {
                Logger.Info("Category words loaded.");
                _isFirstCategoriesLoad = false;
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_ignoreEvents.IsActive)
                return;

            bool? hasSuggestions = RefreshSuggestions(false, true);
            if (hasSuggestions == false && ControlsHelper.HasTextBecomeLonger(e))
            {
                SystemSounds.Beep.Play();
            }
        }

        private void ProcessWordCategoryChanged(int wordId, Guid categoryId, bool isRemoved)
        {
            var category = cboCategories.SelectedItem as DictionaryCategoryListItem;
            if (category == null || category.IsServiceItem)
                return;

            // If current category matches the modified one we should refresh suggestions list
            if (categoryId == category.CategoryId)
            {
                int initialPosition = lstSuggestions.SelectedIndex;
                bool restoreFocus = lstSuggestions.IsKeyboardFocusWithin;

                bool? hasSuggestions = RefreshSuggestions(true, false);
                if (hasSuggestions == true)
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
                    if (_autoListsManager.IsAutoList(category.CategoryId))
                    {
                        query = _autoListsManager.ApplyAutoList(category.CategoryId, query);
                    }
                    else
                    {
                        var categoryWords = new HashSet<int>(_categoryManager.GetCategoryWordIds(category.CategoryId));
                        if (categoryWords != null && categoryWords.Count > 0)
                        {
                            query = query.Where(x => x.WordId != null && categoryWords.Contains(x.WordId.Value));
                        }
                        else
                        {
                            // It means there are no words in the category
                            query = new IndexEntry[0];
                        }
                    }
                }

                _searchIndex = new DictionaryIndex();
                _searchIndex.Build(query, false);
            }
        }

        private bool? RefreshSuggestions(bool activateFilter, bool selectFirstItem)
        {
            if (!_dictionaryLoader.IsInitialized)
            {
                lstSuggestions.AttachItemsSource(new[] { GetInitializingSuggestion() });
                _dictionaryLoader.ExecuteOnInitialized(() => RefreshSuggestions(activateFilter, selectFirstItem));
                return null;
            }

            if (activateFilter)
            {
                ActivateFilter();
            }

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
                    bool hasMoreItems = false;
                    if (filterEntries == null || filterEntries.Count < VisibleNumberOfSuggestions)
                    {
                        // Subtract 1 for "Out of the filter" item
                        int maxNumberOfExtraItems = MaxNumberOfSuggestions - filterEntries.Count - 1;
                        extraEntries = _mainIndex.FindEntriesByText(searchText, false, maxNumberOfExtraItems);
                        if (extraEntries != null)
                        {
                            bool addEllipsis = (extraEntries.Count == maxNumberOfExtraItems);
                            if (filterEntries != null && filterEntries.Count > 0)
                            {
                                extraEntries = extraEntries.Where(x => !filterEntries.Contains(x)).ToList();
                            }

                            if (extraEntries.Count > 0)
                            {
                                if (addEllipsis)
                                {
                                    extraEntries.Add(new IndexEntryImitation("..."));
                                    hasMoreItems = true;
                                }
                                extraEntries.Insert(0, new IndexEntryImitation("*** Out of the filter ***"));
                            }
                        }
                    }

                    lstSuggestions.AttachItemsSource(filterEntries, extraEntries, hasMoreItems);
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
                    bool hasMoreItems = false;
                    List<IndexEntry> filterEntries = _searchIndex.FindEntriesByText(searchText, false, MaxNumberOfSuggestions);
                    if (filterEntries != null && filterEntries.Count == MaxNumberOfSuggestions)
                    {
                        filterEntries.Add(new IndexEntryImitation("..."));
                        hasMoreItems = true;
                    }

                    lstSuggestions.AttachItemsSource(filterEntries, hasMoreItems);
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

        private IndexEntryImitation GetInitializingSuggestion()
        {
            return new IndexEntryImitation("Initializing...");
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
            switch (e.Key)
            {
                case Key.Enter:
                    NavigateSelectedItem(lstSuggestions, NavigationSource.SuggestionsList);
                    break;

                case Key.Delete:
                    ChangeWordCategory(true);
                    break;

                case Key.Insert:
                    ChangeWordCategory(false);
                    break;
            }
        }

        private void lstSuggestions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _commandPrevious.UpdateState(lstSuggestions.CanSelectPrevious);
            _commandNext.UpdateState(lstSuggestions.CanSelectNext);

            RefreshSuggestionStats();
        }

        private void lstSuggestions_ItemsSourceChanged(object sender, EventArgs e)
        {
            RefreshSuggestionStats();
        }

        private void RefreshSuggestionStats()
        {
            int count = lstSuggestions.Items.Count;
            if (lstSuggestions.HasMoreItems)
            {
                count--;
            }

            if (count == 0 || (count == 1 && (lstSuggestions.Items[0] is IndexEntryImitation)))
            {
                lblSuggestionStats.Text = null;
            }
            else if (lstSuggestions.SelectedIndex < 0)
            {
                lblSuggestionStats.Text = string.Format("0 of {0}", count);
            }
            else
            {
                lblSuggestionStats.Text = string.Format("{0} of {1}", lstSuggestions.SelectedIndex + 1, count);
            }
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
            var dialog = new CategoriesList();
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
                if (_dictionaryLoader.IsInitialized && !string.IsNullOrEmpty(txtSearch.Text))
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
            if (article != null && article.ArticleKey == selectedItem.Word.ArticleKey)
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
            if (_isFirstPageLoad)
            {
                Logger.Info("Preparing article page...");
            }

            ArticlePage article = _dictionaryProvider.PrepareArticlePage(sourceIndex);
            if (_isFirstPageLoad)
            {
                Logger.Info("Article page prepared. Navigating...");
            }
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

        private void ChangeWordCategory(bool isRemove)
        {
            var selectedItem = lstSuggestions.SelectedItem as IndexEntry;
            if (selectedItem == null || selectedItem.WordId == null)
                return;

            var category = cboCategories.SelectedItem as DictionaryCategoryListItem;
            if (category == null || category.IsServiceItem)
                return;

            int wordId = selectedItem.WordId.Value;
            bool isChanged = isRemove 
                ? _categoryManager.RemoveCategory(wordId, category.CategoryId)
                : _categoryManager.AddCategory(wordId, category.CategoryId);
            if (isChanged)
            {
                if (_categoryTracker.CurrentWordId == wordId)
                {
                    _categoryTracker.RefreshCategoryState(category.CategoryId, !isRemove);
                }
            }
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
                new UsageRankListItem(6000, "Top 3000-6000"),
                new UsageRankListItem(9000, "Top 6000-9000")
            };
        }

        private void PopulateCategories()
        {
            var dbCategories = _categoryManager.GetAllCategories();
            _categoryTracker.SynchronizeCategories(dbCategories);
            categoriesDataGrid.ItemsSource = _categoryTracker.GetCategories();

            var allCategories = new List<DictionaryCategoryListItem>();
            allCategories.Add(new DictionaryCategoryListItem("none"));
            allCategories.Add(new DictionaryCategoryListItem());
            if (dbCategories.Count(x => x.IsTopCategory) > 0)
            {
                allCategories.AddRange(dbCategories.Where(x => x.IsTopCategory)
                    .OrderBy(x => x).Select(x => new DictionaryCategoryListItem(x)));
                allCategories.Add(new DictionaryCategoryListItem());
            }

            var systemCategories = new List<DictionaryCategoryItem>
            {
                new DictionaryCategoryItem { DisplayName = "Words with notes", IsSystemCategory = true, 
                    CategoryId = AutoListsManager.WordsWithNotes },
                new DictionaryCategoryItem { DisplayName = "Top multi-pronunciation words", IsSystemCategory = true,
                    CategoryId = AutoListsManager.WordsWithMultiplePronunciations }     
                //new DictionaryCategoryItem { DisplayName = "Recent recordings", IsSystemCategory = true,
                //    CategoryId = AutoListsManager.RecentRecordings },           
            };
            systemCategories.AddRange(dbCategories.Where(x => x.IsSystemCategory && !x.IsTopCategory));
            systemCategories.Sort();
            allCategories.AddRange(systemCategories.Select(x => new DictionaryCategoryListItem(x)));

            if (dbCategories.Count(x => !x.IsSystemCategory && !x.IsTopCategory) > 0)
            {
                allCategories.Add(new DictionaryCategoryListItem());
                allCategories.AddRange(dbCategories.Where(x => !x.IsSystemCategory && !x.IsTopCategory)
                    .OrderBy(x => x).Select(x => new DictionaryCategoryListItem(x)));
            }

            cboCategories.ItemsSource = allCategories;
        }
    }
}
