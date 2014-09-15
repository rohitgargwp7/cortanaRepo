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

namespace windows_client.Controls
{
    public partial class PinUC : UserControl
    {
        public PinUC()
        {
            InitializeComponent();
        }

        #region Dependency_Properties

        public static readonly DependencyProperty RightIconSourceProperty =
            DependencyProperty.Register("RightIconSource", typeof(ImageSource), typeof(PinUC), new PropertyMetadata(OnRightIconSourceChanged));

        public static void OnRightIconSourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            PinUC tempPin = obj as PinUC;
            ImageSource tempSource = (ImageSource)e.NewValue;
            tempPin.rightIcon.Source = tempSource;
        }

        public ImageSource RightIconSource
        {
            get
            {
                return (ImageSource)GetValue(RightIconSourceProperty);
            }
            set
            {
                SetValue(RightIconSourceProperty, value);
            }
        }
        #endregion

        #region Event_Handlers
        public event EventHandler<EventArgs> RightIconClicked;
        public event EventHandler<EventArgs> NewPinLostFocus;
        public event EventHandler<EventArgs> PinContent_Tapped;

        private void rightIcon_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (RightIconClicked != null)
                RightIconClicked(sender, e);
        }

        private void newPinTxt_LostFocus(object sender, RoutedEventArgs e)
        {
            if (NewPinLostFocus != null)
                NewPinLostFocus(sender, e);
        }

        private void pinContent_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (PinContent_Tapped != null)
                PinContent_Tapped(sender, e);
        }
        #endregion

        private void newPinTxt_GotFocus(object sender, RoutedEventArgs e)
        {
            newPinTxt.Text = String.Empty;
            newPinTxt.Hint = string.Empty;
            newPinTxt.Hint = "Share something..."; //to be decided constant or Localize
        }

        public void isShow(bool showNewPin, bool showOldPin)
        {
            if (showNewPin || showOldPin)
                Visibility = Visibility.Visible;
            else
                Visibility = Visibility.Collapsed;

            if (showNewPin)
            {
                pinContent.Visibility = Visibility.Collapsed;
                newPinTxt.Visibility = Visibility.Visible;
                rightIcon.Visibility = Visibility.Collapsed;
                newPinTxt.Focus();
            }
            else
            {
                pinContent.Visibility = Visibility.Visible;
                newPinTxt.Visibility = Visibility.Collapsed;
                rightIcon.Visibility = Visibility.Visible;
            }
        }

        public void updateContent(string contact, string message)
        {
            if (!String.IsNullOrWhiteSpace(contact) && !String.IsNullOrWhiteSpace(message))
            {
                pinContactName.Text = contact;
                pinTxt.Text = message;
            }
        }

        public string GetNewPinMessage()
        {
            return newPinTxt.Text.Trim();
        }
    }
}