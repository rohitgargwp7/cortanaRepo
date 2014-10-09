using Microsoft.Phone.Networking.Voip;
using Microsoft.Phone.Scheduler;
using System;
using System.Diagnostics;
using System.Windows;

namespace windows_client.utils
{
    public class VoipBackgroundAgent
    {
        // The name of the incoming call task.
        private const string incomingCallTaskName = "HikeVoipBackgroundAgent";

        public static void InitVoipBackgroundAgent()
        {
            // Obtain a reference to the existing task, if any.
            VoipHttpIncomingCallTask voipBackgroundTask = ScheduledActionService.Find(incomingCallTaskName) as VoipHttpIncomingCallTask;
            if (voipBackgroundTask != null)
            {
                if (voipBackgroundTask.IsScheduled == false)
                {
                    // The incoming call task has been unscheduled due to OOM or throwing an unhandled exception twice in a row
                    ScheduledActionService.Remove(incomingCallTaskName);
                }
                else
                {
                    // The incoming call task has been scheduled and is still scheduled so there is nothing more to do
                    return;
                }
            }

            // Create a new incoming call task.
            voipBackgroundTask = new VoipHttpIncomingCallTask(incomingCallTaskName, HikeConstants.PushNotificationChannelName);
            voipBackgroundTask.Description = "Incoming call task";
            try
            {
                ScheduledActionService.Add(voipBackgroundTask);
            }
            catch (InvalidOperationException exception)
            {
                if (exception.Message.Contains("BNS Error: The action is disabled"))
                {
                    MessageBox.Show("Background agents for this application have been disabled by the user.Some functions might not work properly. Please turn on background agents.");
                }

                if (exception.Message.Contains("BNS Error: The maximum number of ScheduledActions of this type have already been added."))
                {
                    // No user action required. The system prompts the user when the hard limit of periodic tasks has been reached.
                }
            }
            catch (SchedulerServiceException)
            {
                Debug.WriteLine("VoipBackgroundAgent::InitHttpNotificationTask ");
                // No user action required.
            }
        }
    }
}
