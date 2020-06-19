using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Parser.Pages;

namespace Parser
{
    public partial class App : Application
    {
        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
        }

        private void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs unobservedTaskExceptionEventArgs)
        {
            var exception = unobservedTaskExceptionEventArgs.Exception as Exception;
            if (exception != null)
            {
                Current.Dispatcher.Invoke((Action)delegate {
                    /*var viewer = new ExceptionViewer("Parse.Exception.", exception);
                    viewer.ShowDialog();*/
                });
            }
        }

        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            var exception = unhandledExceptionEventArgs.ExceptionObject as Exception;
            if (exception != null)
            {
                if (!(exception is Jint.Runtime.JavaScriptException))
                {
                    Current.Dispatcher.Invoke((Action)delegate {
                        /*var viewer = new ExceptionViewer("Parse.Exception.", exception).ShowDialog();*/
                    });
                }
            }
        }
    }
}
