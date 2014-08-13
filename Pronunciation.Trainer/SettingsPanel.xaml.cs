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
using System.Windows.Shapes;
using Pronunciation.Core;

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for SettingsPanel.xaml
    /// </summary>
    public partial class SettingsPanel : UserControl
    {
        public SettingsPanel()
        {
            InitializeComponent();
            DataContext = new AppSettingsWrapper();
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            pnlDays.Visibility = Visibility.Hidden;
        }

        private void cboRecordingHistoryMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = cboRecordingHistoryMode.SelectedItem as KeyTextPair<RecordingHistoryMode>;
            pnlDays.Visibility = (selectedItem != null && selectedItem.Key == RecordingHistoryMode.OverrideLatestAfterNDays)
                ? Visibility.Visible : Visibility.Hidden;
        }
    }

    public class AppSettingsWrapper
    {
        public AppSettings Settings { get; private set; }
        
        public KeyTextPair<StartupPlayMode>[] StartupEntries { get; private set; }
        public KeyTextPair<RecordedPlayMode>[] RecordingEntries { get; private set; }
        public KeyTextPair<RecordingHistoryMode>[] HistoryEntries { get; private set; }
 
        public AppSettingsWrapper()
        {
            Settings = AppSettings.Instance;

            StartupEntries = new KeyTextPair<StartupPlayMode>[] 
                { 
                    new KeyTextPair<StartupPlayMode>(StartupPlayMode.None, "Don't play anything"),
                    new KeyTextPair<StartupPlayMode>(StartupPlayMode.British, "Play British pronunciation"),
                    new KeyTextPair<StartupPlayMode>(StartupPlayMode.American, "Play American pronunciation")
                };

            RecordingEntries = new KeyTextPair<RecordedPlayMode>[] 
                {
                    new KeyTextPair<RecordedPlayMode>(RecordedPlayMode.RecordedOnly, "Play recorded audio"),
                    new KeyTextPair<RecordedPlayMode>(RecordedPlayMode.RecordedThenReference, "Play recorded, then reference audio"),
                    new KeyTextPair<RecordedPlayMode>(RecordedPlayMode.ReferenceThenRecorded, "Play reference, then recorded audio")
                };

            HistoryEntries = new KeyTextPair<RecordingHistoryMode>[] 
                {
                    new KeyTextPair<RecordingHistoryMode>(RecordingHistoryMode.AlwaysOverrideLatest, "Always override latest recording"),
                    new KeyTextPair<RecordingHistoryMode>(RecordingHistoryMode.OverrideLatestAfterNDays, "Override latest recording if it's newer than N days"),
                    new KeyTextPair<RecordingHistoryMode>(RecordingHistoryMode.AlwaysAdd, "Always add a new recording")
                };
        }
    }
}
