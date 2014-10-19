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
using System.Windows.Shapes;
using Pronunciation.Trainer.Utility;
using System.ComponentModel;
using Pronunciation.Core.Database;

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for EditWordNotes.xaml
    /// </summary>
    public partial class WordNotes : Window
    {
        public delegate void WordInfoUpdatedDelegate(DictionaryWord wordDetails);

        private Entities _dbContext;
        private Lazy<DictionaryWord> _word;

        public int WordId { get; set; }
        public event WordInfoUpdatedDelegate WordInfoUpdated;

        public WordNotes()
        {
            InitializeComponent();
        }

        public DictionaryWord WordDetails
        {
            get { return _word.Value; }
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            _dbContext = new Entities();
            _word = new Lazy<DictionaryWord>(() => _dbContext.DictionaryWords.Single(x => x.WordId == WordId));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            lblWordName.Text = WordDetails.Keyword;
            transcriptionTextBox.Focus();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // This will commit latest changes in case if focus is in a textbox
            btnCancel.Focus();

            if (_dbContext.HasChanges())
            {
                if (!MessageHelper.ShowConfirmation(
                    "You have some pending changes. Are you sure you want to discard them?",
                    "Confirm discarding changes"))
                {
                    e.Cancel = true;
                    return;
                }
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            // In case user pressed "Enter" and current focus is in a textbox
            btnOK.Focus();

            if (_dbContext.HasChanges())
            {
                var word = WordDetails;
                word.HasNotes = !string.IsNullOrWhiteSpace(word.FavoriteTranscription)
                    || !string.IsNullOrWhiteSpace(word.Notes);

                _dbContext.SaveChanges();
                if (WordInfoUpdated != null)
                {
                    WordInfoUpdated(WordDetails);
                }
            }

            if (ControlsHelper.IsModalWindow)
            {
                this.DialogResult = true;
            }
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (ControlsHelper.IsExplicitCloseRequired(btnCancel))
            {
                this.Close();
            }
        }
    }
}
