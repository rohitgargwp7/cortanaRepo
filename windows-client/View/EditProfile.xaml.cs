﻿using System;
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
using System.Text.RegularExpressions;
using windows_client.Languages;
using System.Diagnostics;

namespace windows_client.View
{
    public partial class EditProfile : PhoneApplicationPage
    {
        bool isClicked = false;
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
            App.appSettings.TryGetValue(HikeConstants.GENDER, out userGender);

            if (userGender == "m" || userGender == "f")
            {
                genderList.Add(AppResources.EditProfile_GenderMale_LstPckr);
                genderList.Add(AppResources.EditProfile_GenderFemale_lstPckr);
            }
            else // nothing is selected
            {
                genderList.Add(AppResources.EditProfile_GenderSelect_LstPckr);
                genderList.Add(AppResources.EditProfile_GenderMale_LstPckr);
                genderList.Add(AppResources.EditProfile_GenderFemale_lstPckr);
            }

            genderListPicker.ItemsSource = genderList;
            TiltEffect.TiltableItems.Add(typeof(ListPickerItem));
            prepopulate();
            appBar = new ApplicationBar()
            {
                ForegroundColor = ((SolidColorBrush)App.Current.Resources["AppBarForeground"]).Color,
                BackgroundColor = ((SolidColorBrush)App.Current.Resources["AppBarBackground"]).Color,
            };

            nextIconButton = new ApplicationBarIconButton();
            nextIconButton.IconUri = new Uri("/View/images/AppBar/icon_save.png", UriKind.Relative);
            nextIconButton.Text = AppResources.Save_AppBar_Btn;
            nextIconButton.Click += new EventHandler(doneBtn_Click);
            nextIconButton.IsEnabled = true;
            appBar.Buttons.Add(nextIconButton);
            ApplicationBar = appBar;
        }

        private void prepopulate()
        {
            App.appSettings.TryGetValue(HikeConstants.ACCOUNT_NAME, out userName);
            name.Text = string.IsNullOrWhiteSpace(userName) ? string.Empty : userName;

            phone.Text = App.MSISDN;

            if (App.appSettings.Contains(HikeConstants.EMAIL))
                userEmail = (string)App.appSettings[HikeConstants.EMAIL];
            email.Text = userEmail;

            if (userGender == "m")
                genderListPicker.SelectedIndex = 0;
            else if (userGender == "f")
                genderListPicker.SelectedIndex = 1;

            genderIndex = genderListPicker.SelectedIndex;
        }

        private void doneBtn_Click(object sender, EventArgs e)
        {
            if (isClicked)
                return;
            isClicked = true;
            shouldSendProfile = false;
            nameErrorTxt.Opacity = 0;
            emailErrorTxt.Opacity = 0;

            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBoxResult result = MessageBox.Show(AppResources.Please_Try_Again_Txt, AppResources.No_Network_Txt, MessageBoxButton.OK);
                isClicked = false;
                return;
            }

            this.Focus(); // this will hide keyboard
            shellProgress.IsIndeterminate = true;

            // if name is empty simply dont do anything
            if (string.IsNullOrWhiteSpace(name.Text))
            {
                nameErrorTxt.Opacity = 1;
                shellProgress.IsIndeterminate = false;
                isClicked = false;
                return;
            }

            JObject obj = new JObject();
            if (userEmail != email.Text) // if email is changed
            {
                if (ValidateEmail(email.Text)) // check if email is valid
                {
                    obj[HikeConstants.EMAIL] = email.Text;
                    shouldSendProfile = true;
                }
                else //if email is not valid
                {
                    emailErrorTxt.Opacity = 1;
                    shellProgress.IsIndeterminate = false;
                    isClicked = false;
                    return;
                }
            }
            // if gender is changed
            if (genderIndex != genderListPicker.SelectedIndex)
            {
                obj[HikeConstants.GENDER] = genderListPicker.Items.Count == 3 ? (genderListPicker.SelectedIndex == 1 ? "m" : genderListPicker.SelectedIndex == 2 ? "f" : "") : (genderListPicker.SelectedIndex == 0 ? "m" : genderListPicker.SelectedIndex == 1 ? "f" : "");
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
                    MessageBox.Show(AppResources.EditProfile_UpdatErrMsgBx_Text, AppResources.EditProfile_UpdatErrMsgBx_Captn, MessageBoxButton.OK);
                }
                //progressBar.IsEnabled = false;
                shellProgress.IsIndeterminate = false;
                isClicked = false;
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
                if (obj != null && HikeConstants.OK == (string)obj[HikeConstants.STAT])
                {
                    if (userName != name.Text)
                    {
                        userName = name.Text;
                        App.HikePubSubInstance.publish(HikePubSub.UPDATE_ACCOUNT_NAME, userName);
                        App.WriteToIsoStorageSettings(HikeConstants.ACCOUNT_NAME, userName);

                        // this will handle tombstine case too, if we have used pubsub that will not work in case of tombstone
                        PhoneApplicationService.Current.State[HikeConstants.PROFILE_NAME_CHANGED] = userName;
                    }
                    if (shouldSendProfile)
                    {
                        AccountUtils.setProfile(obj, new AccountUtils.postResponseFunction(setProfile_Callback));
                    }
                    else
                    {
                        MakeFieldsReadOnly(false);
                        //progressBar.IsEnabled = false;
                        shellProgress.IsIndeterminate = false;
                        try
                        {
                            MessageBox.Show(AppResources.EditProfile_UpdatMsgBx_Txt, AppResources.EditProfile_UpdatMsgBx_Captn, MessageBoxButton.OK);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Edit Profile ::  setName_Callback, setName_Callback_Okay  , Exception : " + ex.StackTrace);
                        }
                    }
                }
                else
                {
                    MakeFieldsReadOnly(false);
                    shellProgress.IsIndeterminate = false;
                    try
                    {
                        MessageBox.Show(AppResources.EditProfile_NameUpdateErr_MsgBxTxt, AppResources.EditProfile_NameUpdateErr_MsgBxCaptn, MessageBoxButton.OK);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Edit Profile ::  setName_Callback, setName_Callback_Okay  , Exception : " + ex.StackTrace);
                    }
                }
                isClicked = false;
            });
        }

        private void setProfile_Callback(JObject obj)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (obj != null && HikeConstants.OK == (string)obj[HikeConstants.STAT])
                {

                    if (userEmail != email.Text)
                    {
                        userEmail = email.Text;
                        App.WriteToIsoStorageSettings(HikeConstants.EMAIL, email.Text);
                    }

                    if (genderIndex != genderListPicker.SelectedIndex)
                    {
                        genderIndex = genderListPicker.SelectedIndex;
                        string gender = genderListPicker.Items.Count == 3 ? (genderListPicker.SelectedIndex == 1 ? "m" : genderListPicker.SelectedIndex == 2 ? "f" : "") : (genderListPicker.SelectedIndex == 0 ? "m" : "f");
                        App.WriteToIsoStorageSettings(HikeConstants.GENDER, gender);

                        if (genderListPicker.Items.Count == 3) // if select is there remove it
                        {
                            genderListPicker.ItemsSource = new List<string> { AppResources.EditProfile_GenderMale_LstPckr, AppResources.EditProfile_GenderFemale_lstPckr };
                            genderIndex = gender == "m" ? 0 : 1;
                        }
                    }
                    MakeFieldsReadOnly(false);
                    //progressBar.IsEnabled = false;
                    //progressBar.Opacity = 0;
                    shellProgress.IsIndeterminate = false;
                    try
                    {
                        MessageBox.Show(AppResources.EditProfile_UpdatMsgBx_Txt, AppResources.EditProfile_UpdatMsgBx_Captn, MessageBoxButton.OK);

                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Edit Profile ::  setProfile_Callback, setProfile_Callback_okay  , Exception : " + ex.StackTrace);
                    }
                }
                else // failure from server
                {
                    MakeFieldsReadOnly(false);
                    if (App.appSettings.Contains(HikeConstants.EMAIL))
                        email.Text = (string)App.appSettings[HikeConstants.EMAIL];

                    shellProgress.IsIndeterminate = false;
                    
                    try
                    {
                        MessageBox.Show(AppResources.EditProfile_EmailUpdateErr_MsgBxTxt, AppResources.EditProfile_NameUpdateErr_MsgBxCaptn, MessageBoxButton.OK);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Edit Profile ::  setProfile_Callback, setProfile_Callback , Exception : " + ex.StackTrace);
                    }

                }
                isClicked = false;
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

            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.New || App.IS_TOMBSTONED)
            {
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

        private void name_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                email.Focus();
            }
        }

        private void email_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                genderListPicker.Focus();
            }

        }

        public static bool ValidateEmail(string str)
        {
            // Return true if strIn is in valid e-mail format.
            return Regex.IsMatch(str, @"\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*");
        }
    }
}