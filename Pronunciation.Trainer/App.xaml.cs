using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

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

            BindingErrorTraceListener.SetTrace();
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            MessageBox.Show(e.Exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show((e.ExceptionObject as Exception).Message, "Thread error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
