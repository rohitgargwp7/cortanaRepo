using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Newtonsoft.Json.Linq;
using windows_client.utils;
using System.Net.NetworkInformation;
using System.Threading;

namespace windows_client.View
{
    public partial class EditProfile : PhoneApplicationPage
    {
        private ApplicationBar appBar;
        ApplicationBarIconButton nextIconButton;
        List<string> genderList = new List<string>(3);

        string userName = null;
        string userEmail = string.Empty;
        string userGender = null;
        private int genderIndex = 0;
        private bool shouldSendProfile = false;

        public EditProfile()
        {
            InitializeComponent();
            genderList.Add("Select");
            genderList.Add("Male");
            genderList.Add("Female");
            genderListPicker.ItemsSource = genderList;
            TiltEffect.TiltableItems.Add(typeof(ListPickerItem));
            prepopulate();
            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.Opacity = 1;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = false;

            nextIconButton = new ApplicationBarIconButton();
            nextIconButton.IconUri = new Uri("/View/images/icon_save.png", UriKind.Relative);
            nextIconButton.Text = "save";
            nextIconButton.Click += new EventHandler(doneBtn_Click);
            nextIconButton.IsEnabled = true;
            appBar.Buttons.Add(nextIconButton);
            editProfile.ApplicationBar = appBar;
        }

        private void prepopulate()
        {
            App.appSettings.TryGetValue(App.ACCOUNT_NAME, out userName);
            name.Text = string.IsNullOrWhiteSpace(userName) ? string.Empty : userName;

            phone.Text = App.MSISDN;

            if (App.appSettings.Contains(App.EMAIL))
                userEmail = (string)App.appSettings[App.EMAIL];
            email.Text = userEmail;

            App.appSettings.TryGetValue(App.GENDER, out userGender);

            if (userGender == "m")
                genderListPicker.SelectedIndex = 1;
            else if (userGender == "f")
                genderListPicker.SelectedIndex = 2;
            genderIndex = genderListPicker.SelectedIndex;
        }

        private void doneBtn_Click(object sender, EventArgs e)
        {
            shouldSendProfile = false;
            nameErrorTxt.Opacity = 0;
            emailErrorTxt.Opacity = 0;

            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBoxResult result = MessageBox.Show("Connection Problem. Try Later!!", "Oops, something went wrong!", MessageBoxButton.OK);
                return;
            }
            this.Focus(); // this will hide keyboard
            progressBar.IsEnabled = true;
            progressBar.Opacity = 1;

            // if name is empty simply dont do anything
            if (string.IsNullOrWhiteSpace(name.Text))
            {
                nameErrorTxt.Opacity = 1;
                progressBar.IsEnabled = false;
                progressBar.Opacity = 0;
                return;
            }

            JObject obj = new JObject();
            if (userEmail != email.Text) // if email is changed
            {
                if (true) // check if email is valid
                {
                    obj[App.EMAIL] = email.Text;
                    shouldSendProfile = true;
                }
                else //if email is not valid
                {
                    emailErrorTxt.Opacity = 1;
                    progressBar.IsEnabled = false;
                    progressBar.Opacity = 0;
                    return;
                }
            }
            // if gender is changed
            if (genderIndex != genderListPicker.SelectedIndex)
            {
                //genderIndex = genderListPicker.SelectedIndex;
                obj[App.GENDER] = genderListPicker.SelectedIndex == 1 ? "m" : genderListPicker.SelectedIndex == 2 ? "f" : "";
                shouldSendProfile = true;
            }

            if (userName != name.Text) // shows name value is changed
            {
                MakeFieldsReadOnly(true);
                AccountUtils.setName(name.Text, new AccountUtils.postResponseFunction(setName_Callback));
            }
            else if (shouldSendProfile) // send if anything has changed
            {
                MakeFieldsReadOnly(true);
                AccountUtils.setProfile(obj, new AccountUtils.postResponseFunction(setProfile_Callback));
            }
            else // if nothing is changed do nothing
            {
                if (userName == name.Text)
                {
                    MessageBox.Show("Nothing is changed!!", "Profile Not Updated!", MessageBoxButton.OK);
                }
                progressBar.IsEnabled = false;
                progressBar.Opacity = 0;
            }
        }

        private void MakeFieldsReadOnly(bool isReadOnly)
        {
            if (isReadOnly)
            {
                name.IsReadOnly = true;
                email.IsReadOnly = true;
                genderListPicker.IsEnabled = false;
            }
            else
            {
                name.IsReadOnly = false;
                email.IsReadOnly = false;
                genderListPicker.IsEnabled = true;
            }
        }

        private void setName_Callback(JObject obj)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (obj != null && "ok" == (string)obj["stat"])
                {
                    if (userName != name.Text)
                    {
                        userName = name.Text;
                        App.HikePubSubInstance.publish(HikePubSub.UPDATE_ACCOUNT_NAME, userName);
                        App.WriteToIsoStorageSettings(App.ACCOUNT_NAME, userName);
                    }
                    if (shouldSendProfile)
                    {
                        AccountUtils.setProfile(obj, new AccountUtils.postResponseFunction(setProfile_Callback));
                    }
                    else
                    {
                        MakeFieldsReadOnly(false);
                        progressBar.IsEnabled = false;
                        progressBar.Opacity = 0;
                        MessageBox.Show("Profile Has been updated successfully.", "Profile Updated.", MessageBoxButton.OK);
                    }
                }
                else
                {
                    MakeFieldsReadOnly(false);
                    progressBar.IsEnabled = false;
                    progressBar.Opacity = 0;
                    MessageBox.Show("Unable to change profile. Try Later!!", "Oops, something went wrong!", MessageBoxButton.OK);
                }
            });
        }

        private void setProfile_Callback(JObject obj)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (obj != null && "ok" == (string)obj["stat"])
                {

                    if (userEmail != email.Text)
                    {
                        userEmail = email.Text;
                        App.WriteToIsoStorageSettings(App.EMAIL, email.Text);
                    }

                    if (genderIndex != genderListPicker.SelectedIndex)
                    {
                        genderIndex = genderListPicker.SelectedIndex;
                        App.WriteToIsoStorageSettings(App.GENDER, genderListPicker.SelectedIndex == 1 ? "m" : genderListPicker.SelectedIndex == 2 ? "f" : "");
                    }
                    MakeFieldsReadOnly(false);
                    progressBar.IsEnabled = false;
                    progressBar.Opacity = 0;
                    MessageBox.Show("Profile Has been updated successfully.", "Profile Updated.", MessageBoxButton.OK);
                }
                else // failure from server
                {
                    MakeFieldsReadOnly(false);
                    progressBar.IsEnabled = false;
                    progressBar.Opacity = 0;
                    MessageBox.Show("Unable to change email/gender. Try Later!!", "Oops, something went wrong!", MessageBoxButton.OK);
                    
                }
            });
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            this.State["nameErrorTxt.Opacity"] = (int)nameErrorTxt.Opacity;
            this.State["emailErrorTxt.Opacity"] = (int)emailErrorTxt.Opacity;
            this.State["name.Text"] = name.Text;
            this.State["email.Text"] = email.Text;
            this.State["genderListPicker.SelectedIndex"] = genderListPicker.SelectedIndex;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (this.State.ContainsKey("nameErrorTxt.Opacity"))
                nameErrorTxt.Opacity = (int)this.State["nameErrorTxt.Opacity"];

            if (this.State.ContainsKey("emailErrorTxt.Opacity"))
                emailErrorTxt.Opacity = (int)this.State["emailErrorTxt.Opacity"];

            if (this.State.ContainsKey("name.Text"))
                name.Text = (string)this.State["name.Text"];

            if (this.State.ContainsKey("email.Text"))
                email.Text = (string)this.State["email.Text"];

            if (this.State.ContainsKey("genderListPicker.SelectedIndex"))
                genderListPicker.SelectedIndex = (int)this.State["genderListPicker.SelectedIndex"];
        }
    }
}