using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Markup;
using System.Data.Entity.Validation;

namespace Pronunciation.Trainer.Utility
{
    public static class MessageHelper
    {
        public static void ShowInfo(string message)
        {
            ShowInfo(message, null);
        }

        public static void ShowInfo(string message, string title)
        {
            MessageBox.Show(message,
                string.IsNullOrEmpty(title) ? "Pronunciation Trainer" : title,
                MessageBoxButton.OK);
        }

        public static void ShowWarning(string message)
        {
            ShowWarning(message, null);
        }

        public static void ShowWarning(string message, string title)
        {
            MessageBox.Show(message,
                string.IsNullOrEmpty(title) ? "Warning" : title,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        public static void ShowError(string message)
        {
            ShowError(message, null);
        }

        public static void ShowError(string message, string title)
        {
            MessageBox.Show(
                string.IsNullOrEmpty(message) ? "Unknown error" : message, 
                string.IsNullOrEmpty(title) ? "Error" : title, 
                MessageBoxButton.OK, 
                MessageBoxImage.Error);
        }

        public static void ShowError(Exception ex)
        {
            ShowError(ex, null);
        }

        public static void ShowError(Exception ex, string title)
        {
            ShowError(PrepareErrorMessage(ex), title);
        }

        public static bool ShowConfirmation(string message)
        {
            return ShowConfirmation(message, null, false);
        }

        public static bool ShowConfirmation(string message, string title)
        {
            return ShowConfirmation(message, title, false);
        }

        public static bool ShowConfirmation(string message, string title, bool isDefaultYes)
        {
            var result = MessageBox.Show(message, 
                string.IsNullOrEmpty(title) ? "Confirmation required" : title, 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question,
                isDefaultYes ? MessageBoxResult.Yes : MessageBoxResult.No);

            return result == MessageBoxResult.Yes;
        }

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

        private static string PrepareErrorMessage(Exception ex)
        {
            if (ex == null)
                return null;

            string message;
            if (ex is DbEntityValidationException)
            {
                message = ProcessEntityValidationException((DbEntityValidationException)ex);
            }
            else
            {
                message = ex.Message;
            }

            return message;
        }

        private static string ProcessEntityValidationException(DbEntityValidationException ex)
        {
            if (ex.EntityValidationErrors == null)
                return null;

            var messages = new StringBuilder();
            foreach (DbEntityValidationResult result in ex.EntityValidationErrors)
            {
                if (result.ValidationErrors == null)
                    continue;

                bool isFirst = true;
                foreach (DbValidationError error in result.ValidationErrors)
                {
                    if (!isFirst)
                    {
                        messages.AppendLine();
                        isFirst = false;
                    }
                    messages.Append(error.ErrorMessage);
                }
            }

            return messages.ToString();
        }
    }
}
