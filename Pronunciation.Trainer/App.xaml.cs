using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Markup;
using System.Globalization;
using Pronunciation.Trainer.Utility;
using Pronunciation.Core.Utility;

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // To handle exceptions from all threads:
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            
            // For main UI thread: Application.Current.DispatcherUnhandledException or Dispatcher.UnhandledException
            // For async tasks: TaskScheduler.UnobservedTaskException 
            //
            // To dispatch exception from a thread to the main UI thread:
            // System.Windows.Application.Current.Dispatcher.Invoke(
            //    System.Windows.Threading.DispatcherPriority.Normal,
            //    new Action<Exception>((exc) =>
            //    {
            //        throw new Exception("Exception from another Thread", exc);
            //    }), ex);

            // Set "|DataDirectory|" parameter used in the connection string in App.config
            AppDomain.CurrentDomain.SetData("DataDirectory", AppSettings.Instance.Folders.Database);

            // Set current culture for all controls (to display dates correctly)
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement), 
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            BindingErrorTraceListener.SetTrace();

            Logger.Initialize(AppSettings.Instance.Files.Log);
            Logger.Info("\r\n\r\n*** Application started. ***");
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            MessageHelper.ShowError(e.Exception);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageHelper.ShowError(e.ExceptionObject as Exception);
        }
    }
}
