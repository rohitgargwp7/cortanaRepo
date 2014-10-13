using Microsoft.Phone.Networking.Voip;
using Microsoft.Phone.Scheduler;
using System;
using System.Diagnostics;
using System.Windows;
using windows_client.Languages;

namespace windows_client.utils
{
    public class VoipBackgroundAgentHelper
    {
        /// <summary>
        /// Function to subscribe for a background voip agent
        /// </summary>
        public static void InitVoipBackgroundAgent()
        {
            // Obtain a reference to the existing task, if any.
            VoipHttpIncomingCallTask voipBackgroundTask = ScheduledActionService.Find(HikeConstants.VoipBackgroundTaskName) as VoipHttpIncomingCallTask;
            if (voipBackgroundTask != null)
            {
                if (voipBackgroundTask.IsScheduled == false)
                {
                    // The incoming call task has been unscheduled due to OOM or throwing an unhandled exception twice in a row
                    ScheduledActionService.Remove(HikeConstants.VoipBackgroundTaskName);
                }
                else
                {
                    // The incoming call task has been scheduled and is still scheduled so there is nothing more to do
                    return;
                }
            }

            // Create a new incoming call task.
            voipBackgroundTask = new VoipHttpIncomingCallTask(HikeConstants.VoipBackgroundTaskName, HikeConstants.PushNotificationChannelName);
            
            try
            {
                ScheduledActionService.Add(voipBackgroundTask);
            }
            catch (InvalidOperationException exception)
            {
                if (exception.Message.Contains("BNS Error: The action is disabled"))
                {
                    MessageBox.Show(AppResources.BackgroundAgent_Off_Exception_Message);
                }

                if (exception.Message.Contains("BNS Error: The maximum number of ScheduledActions of this type have already been added."))
                {
                }
            }
            catch (SchedulerServiceException exception)
            {
                Debug.WriteLine("VoipBackgroundAgent::InitHttpNotificationTask , Exceptin at : " + exception.StackTrace);
            }
        }

        public static void UnsubscibeVoipBackgroundAgent()
        {
            VoipHttpIncomingCallTask voipBackgroundTask = ScheduledActionService.Find(HikeConstants.VoipBackgroundTaskName) as VoipHttpIncomingCallTask;
            
            if (voipBackgroundTask != null)
                 ScheduledActionService.Remove(HikeConstants.VoipBackgroundTaskName);

        }
    }
}
