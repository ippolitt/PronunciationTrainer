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
using Pronunciation.Trainer.Controls;
using System.IO;
using Pronunciation.Core.Providers.Theory;
using Pronunciation.Trainer.Utility;

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for Theory.xaml
    /// </summary>
    public partial class Theory : UserControlExt
    {
        private TheoryProvider _provider;

        public Theory()
        {
            InitializeComponent();
        }

        private void UserControlExt_Initialized(object sender, EventArgs e)
        {
            _provider = new TheoryProvider(AppSettings.Instance.Folders.Theory);
            lstTopics.ItemsSource = _provider.GetTopics();
            lstTopics.SelectedIndex = 0;
        }

        private void UserControlExt_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void lstTopics_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadTopicContent(lstTopics.SelectedItem as TheoryTopicInfo);
        }

        private void lstTopics_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            LoadTopicContent(lstTopics.SelectedItem as TheoryTopicInfo);
        }

        private void LoadTopicContent(TheoryTopicInfo topic)
        {
            if (topic == null)
            {
                imgContent.Source = null;
            }
            else
            {
                imgContent.Source = ControlsHelper.ImageFromUrl(new Uri(topic.SourceFilePath));
            }
        }
    }
}
