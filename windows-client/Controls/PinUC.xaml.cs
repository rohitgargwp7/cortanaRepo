using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media;
using System.Windows.Media.Animation;
using windows_client.Languages;

namespace windows_client.Controls
{
    public partial class PinUC : UserControl
    {
        public PinUC()
        {
            InitializeComponent();
        }

        #region Event_Handlers
        public event EventHandler<EventArgs> RightIconClicked;
        public event EventHandler<EventArgs> NewPinLostFocus;
        public event EventHandler<EventArgs> PinContentTapped;

        private void rightIcon_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (RightIconClicked != null)
                RightIconClicked(sender, e);
        }

        private void newPinTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (NewPinLostFocus != null)
                NewPinLostFocus(sender, e);
        }

        private void pinContent_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (PinContentTapped != null)
                PinContentTapped(sender, e);
        }
        #endregion

        /// <summary>
        /// Hint has to be reset otherwise it will not show hint initially
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void newPinTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            newPinTextBox.Text = String.Empty;
            newPinTextBox.Hint = String.Empty;
            newPinTextBox.Hint = AppResources.NewGCPin_Hint_Txt;
        }

        /// <summary>
        /// It controls the Visibility and Animation for Pin Control
        /// </summary>
        /// <param name="showNewPin"></param>
        /// <param name="showOldPin"></param>
        /// <param name="forceShow">forcefully displays pin</param>
        public void IsShow(bool showNewPin, bool showOldPin, bool forceShow = false)
        {
            if (showNewPin || showOldPin)
            {
                if (this.Visibility == Visibility.Collapsed || forceShow)
                {
                    this.Visibility = Visibility.Visible;

                    if (forceShow)
                        closePinAnimation.Stop();
                    else
                        openPinAnimation.Begin();
                }
            }
            else
            {
                if (this.Visibility == Visibility.Visible)
                {
                    closePinAnimation.Completed -= closePinAnimation_Completed;
                    closePinAnimation.Completed += closePinAnimation_Completed;
                    closePinAnimation.Begin();
                }
            }

            if (showNewPin)
            {
                pinContent.Visibility = Visibility.Collapsed;
                newPinTextBox.Visibility = Visibility.Visible;
                rightIcon.Visibility = Visibility.Collapsed;
                notificationCountGrid.Visibility = Visibility.Collapsed;
                newPinTextBox.Focus();
            }
            else if (showOldPin)
            {
                pinContent.Visibility = Visibility.Visible;
                rightIcon.Visibility = Visibility.Visible;
                notificationCountGrid.Visibility = String.IsNullOrEmpty(notificationCountTxtBlk.Text) ? Visibility.Collapsed : Visibility.Visible;
                newPinTextBox.Visibility = Visibility.Collapsed;
            }
        }

        void closePinAnimation_Completed(object sender, EventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
        }

        public void UpdateContent(string contact, string message)
        {
            if (!String.IsNullOrWhiteSpace(contact) && !String.IsNullOrWhiteSpace(message))
            {
                pinContactName.Text = contact;
                pinTxt.Text = message;
            }
        }

        public string GetNewPinMessage()
        {
            return newPinTextBox.Text.Trim();
        }

        public void SetUnreadPinCount(int count)
        {
            notificationCountTxtBlk.Text = (count < 10) ? (count == 0 ? String.Empty : count.ToString()) : "9+";
            notificationCountGrid.Visibility = (String.IsNullOrEmpty(notificationCountTxtBlk.Text) || count<=0) ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}