using System.Diagnostics;
using System.Windows;
using Microsoft.Phone.Scheduler;
using System.Threading.Tasks;
using Microsoft.Phone.Shell;
using CommonLibrary.Misc;
using CommonLibrary;

namespace VoipBackgroundAgent
{
    public class ScheduledAgent : ScheduledTaskAgent
    {
        /// <remarks>
        /// ScheduledAgent constructor, initializes the UnhandledException handler
        /// </remarks>
        static ScheduledAgent()
        {
            // Subscribe to the managed exception handler
            Deployment.Current.Dispatcher.BeginInvoke(delegate
            {
                Application.Current.UnhandledException += UnhandledException;
            });
        }

        /// Code to execute on Unhandled Exceptions
        private static void UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                Debugger.Break();
            }
        }

        /// <summary>
        /// Agent that runs a scheduled task
        /// </summary>
        /// <param name="task">
        /// The invoked task
        /// </param>
        /// <remarks>
        /// This method is called when a periodic or resource intensive task is invoked
        /// </remarks>
        protected override void OnInvoke(ScheduledTask task)
        {
            //TODO: Add code to perform your task in background
            RunAgentForSpecifiedTime(45000);
            if (HikeInstantiation.InstantiateClasses())
            {
                //NetworkManager.turnOffNetworkManager = false;
                Debug.WriteLine("Instantiation done ");
            }
            while (true) ;
        }

        public async void RunAgentForSpecifiedTime(int milliseconds)
        {
            Debug.WriteLine("Trying to invoke Notify complete");
            await Task.Delay(milliseconds);
            Debug.WriteLine("Invoking Notify complete");
            NotifyComplete();
        }
    }
}