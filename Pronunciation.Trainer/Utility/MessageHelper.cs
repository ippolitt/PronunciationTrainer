using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Markup;

namespace Pronunciation.Trainer.Utility
{
    public static class MessageHelper
    {
        public static void ShowErrorOnControlInit(Exception ex, Window mainForm)
        {
            var xamlException = ex as XamlParseException;
            string message = (xamlException != null && xamlException.InnerException != null)
                ? xamlException.InnerException.Message : ex.Message;

            Action showDialog = () => MessageBox.Show(mainForm, message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            if (mainForm.IsLoaded)
            {
                showDialog();
            }
            else
            {
                // We can't show MessageBox directly if main form is not loaded yet
                mainForm.Dispatcher.BeginInvoke(showDialog);
            }
        }
    }
}
