using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace Calcpad.Wpf
{
    public partial class App : Application
    {

        public static NamedPipeClient pipeClient;

        private async void App_Startup(object sender, StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += AppDomain_UnhandledException;

            // Initialize and connect the named pipe client asynchronously
            // Make sure to handle exceptions since we can't await in startup event handler

            pipeClient = new NamedPipeClient();

            try
            {
                var connectTask = pipeClient.ConnectAsync();
                await connectTask;
                // Optionally, send a startup message
                var sendMessageTask = pipeClient.SendMessageAsync("GenFEA Client Pipe started...");
                await sendMessageTask;
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occurred during connect or send
                MessageBox.Show($"Error connecting to named pipe server: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Your existing startup logic here
        }


        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            // Perform any cleanup, if necessary
            // For example: pipeClient.Disconnect();
        }

        private void AppDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException -= AppDomain_UnhandledException;
            ReportUnhandledExceptionAndClose((Exception)e.ExceptionObject);
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            DispatcherUnhandledException -= Application_DispatcherUnhandledException;
            e.Handled = true;
            ReportUnhandledExceptionAndClose(e.Exception);
        }

        private static void ReportUnhandledExceptionAndClose(Exception e)
        {

            MainWindow main = (MainWindow)Current.MainWindow;
            var logFileName = Path.GetTempFileName();
            var message = GetMessage(e);
            if (main.IsSaved)
            {
                message += AppMessages.ReportUnhandledExceptionAndClose_NoUnsavedData;
            }
            else
            {
                message += AppMessages.ReportUnhandledExceptionAndClose_NoUnsavedData_RecoveryAttempted;
                try
                {
                    main.SaveStateAndRestart();
                }
                catch
                {
                    message += AppMessages.ReportUnhandledExceptionAndClose_UnsavedData_RecoveryFailed;
                }
            }
            message += string.Format(AppMessages.ExceptionDetails, e);
            File.WriteAllText(logFileName, message);
            Process.Start(new ProcessStartInfo
            {
                FileName = logFileName,
                UseShellExecute = true
            });
            Environment.Exit(System.Runtime.InteropServices.Marshal.GetHRForException((Exception)e));
        }
        private static string GetMessage(Exception e) => string.Format(AppMessages.UnexpectedErrorOccured, e.Message, e.Source);
    }
}