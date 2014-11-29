using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Windows.Phone.Speech.Synthesis;
using Windows.Phone.Speech.Recognition;
using windows_client.Model;
using windows_client.DbUtils;
using windows_client.utils;
using Windows.Phone.Speech.VoiceCommands;
namespace windows_client.View
{
    public partial class CortanaPage : PhoneApplicationPage
    {
        private SpeechSynthesizer speechOutput = new SpeechSynthesizer();
        private SpeechRecognizerUI messageInput = new SpeechRecognizerUI();
        private SpeechRecognizerUI contactInput = new SpeechRecognizerUI();
        private SpeechRecognitionUIResult recoResult;

        public CortanaPage()
        {
            InitializeComponent();
            messageInput.Settings.ListenText  = "Say your message";
            messageInput.Settings.ExampleText = "Good morning";

            contactInput.Settings.ExampleText = "Which contact did you mean?";
            contactInput.Settings.ExampleText = "one";
            contactInput.Recognizer.Grammars.AddGrammarFromList("mainPageCommands", new string[] { "1", "2", "3", "4", "5" ,"6","7","8","9","10"});
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            // Handle the case where the page was launched by Voice Command
            if (this.NavigationContext.QueryString != null && this.NavigationContext.QueryString.ContainsKey("voiceCommandName"))
            {
                // Page was launched by Voice Command
                string commandName = NavigationContext.QueryString["voiceCommandName"];
                string str;

                if (commandName == "ChatWith" && this.NavigationContext.QueryString.TryGetValue("reco", out str))
                {
                    command.Text = str;
                    List<ContactInfo> res = Utils.GetContact(str);

                    if (res.Count == 1)
                        PhoneApplicationService.Current.State["fromcortanapage"] = res[0];
                    else
                    {
                        chooseContactListBox.ItemsSource = res;
                        chooseContactListBox.Visibility = Visibility.Visible;
                        
                        await speechOutput.SpeakTextAsync("Which " + str + " do you want?");
                        recoResult = await contactInput.RecognizeWithUIAsync();
                        
                        int contactPos;
                        
                        if (recoResult.RecognitionResult != null && Int32.TryParse(recoResult.RecognitionResult.Text, out contactPos) && contactPos < res.Count)
                            PhoneApplicationService.Current.State["fromcortanapage"] = res[contactPos - 1];    
                    }

                    await speechOutput.SpeakTextAsync("Say your message");
                    recoResult = await messageInput.RecognizeWithUIAsync();
                    
                    PhoneApplicationService.Current.State["cortanaMessage"] = (recoResult.RecognitionResult == null) ? String.Empty : recoResult.RecognitionResult.Text ;

                    if (PhoneApplicationService.Current.State.ContainsKey("fromcortanapage"))
                        NavigationService.Navigate(new Uri("/View/NewChatThread.xaml",UriKind.Relative));
                }
            }
            base.OnNavigatedTo(e);
        }

        private void StackPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ContactInfo ci = (ContactInfo)((sender as StackPanel).DataContext);

            if (ci == null)
                return;

            PhoneApplicationService.Current.State["fromcortanapage"] = ci;
            PhoneApplicationService.Current.State["cortanaMessage"]=string.Empty;
            NavigationService.Navigate(new Uri("/View/NewChatThread.xaml",UriKind.Relative));
        }
    }
}