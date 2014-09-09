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
        public bool _isNewPin = false;

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

        public event EventHandler<EventArgs> RightIconClicked;
        public event EventHandler<EventArgs> NewPinLostFocus;
        public event EventHandler<EventArgs> PinContent_Tapped;

        private void rightIcon_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (RightIconClicked != null)
                RightIconClicked(sender, e);
        }

        private void newPinTxt_GotFocus(object sender, RoutedEventArgs e)
        {
            newPinTxt.Text = String.Empty;
            newPinTxt.Hint = string.Empty;
            newPinTxt.Hint = "Share something..."; //to be decided constant or Localize
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
    }
}