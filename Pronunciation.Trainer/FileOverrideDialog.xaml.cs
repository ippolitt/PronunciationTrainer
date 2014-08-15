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
using System.IO;

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for FileOverrideDialog.xaml
    /// </summary>
    public partial class FileOverrideDialog : Window
    {
        public enum FileOverrideAction
        {
            Override,
            Skip,
            Abort
        }

        public class FileOverrideResult
        {
            public FileOverrideAction OverrideAction { get; private set; }
            public bool ApplyToNextConflicts { get; private set; }

            public FileOverrideResult(FileOverrideAction overrideAction, bool applyToNextConflicts)
            {
                OverrideAction = overrideAction;
                ApplyToNextConflicts = applyToNextConflicts;
            }
        }

        public FileOverrideResult OverrideResult { get; private set; }
        private const string ConflictsCountTemplate = "Do this for the next {0} conflicts";

        public FileOverrideDialog()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            chkApply.Focus();
        }

        public void InitArguments(string filePath, int nextConflictsCount)
        {
            lblFileName.Text = Path.GetFileName(filePath);
            lblFolder.Text = Path.GetDirectoryName(filePath);
            if (nextConflictsCount > 0)
            {
                chkApply.Visibility = Visibility.Visible;
                chkApply.Content = string.Format(ConflictsCountTemplate, nextConflictsCount); 
            }
            else
            {
                chkApply.Visibility = Visibility.Hidden;
            }
        }

        private void btnOverride_Click(object sender, RoutedEventArgs e)
        {
            CloseDialog(FileOverrideAction.Override);
        }

        private void btnSkip_Click(object sender, RoutedEventArgs e)
        {
            CloseDialog(FileOverrideAction.Skip);
        }

        private void btnAbort_Click(object sender, RoutedEventArgs e)
        {
            CloseDialog(FileOverrideAction.Abort);
        }

        private void CloseDialog(FileOverrideAction action)
        {
            OverrideResult = new FileOverrideResult(action,
                chkApply.Visibility == Visibility.Visible && chkApply.IsChecked == true);

            this.DialogResult = true;
            this.Close();
        }
    }
}
