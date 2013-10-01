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
        }
    }

    public class AppSettingsWrapper
    {
        public AppSettings Settings { get; private set; }
        
        public KeyTextPair<StartupPlayMode>[] StartupEntries { get; private set; }
        public KeyTextPair<RecordedPlayMode>[] RecordingEntries { get; private set; }
 
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
        }
    }
}
