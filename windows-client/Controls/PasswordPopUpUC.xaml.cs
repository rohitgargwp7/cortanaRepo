using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using windows_client.utils;

namespace windows_client.Controls
{
    public partial class PasswordPopUpUC : UserControl
    {
        public PasswordPopUpUC()
        {
            InitializeComponent();
            Visibility = Visibility.Collapsed;
        }

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text", typeof(String), typeof(PasswordPopUpUC), new PropertyMetadata(OnTextChanged));

        public String Text
        {
            get
            {
                return (String)GetValue(TextProperty);
            }
            set
            {
                SetValue(TextProperty, value);
            }
        }

        private static void OnTextChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            PasswordPopUpUC passwordControl = obj as PasswordPopUpUC;
            passwordControl.headingText.Text = (String)e.NewValue;
        }

        public static readonly DependencyProperty IsShowProperty = DependencyProperty.Register(
            "IsShow", typeof(Boolean), typeof(PasswordPopUpUC), new PropertyMetadata(OnIsShowChanged));

        public Boolean IsShow
        {
            get
            {
                return (Boolean)GetValue(IsShowProperty);
            }
            set
            {
                SetValue(IsShowProperty, value);
            }
        }

        private static void OnIsShowChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            PasswordPopUpUC passwordControl = obj as PasswordPopUpUC;
            passwordControl.Visibility = (Boolean)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
            passwordControl.Password = null;

            if (passwordControl.PasswordOverlayVisibilityChanged != null)
                passwordControl.PasswordOverlayVisibilityChanged(passwordControl, null);
        }

        String _password;
        public String Password
        {
            get
            {
                return _password;
            }
            set
            {
                if(value!=_password)
                {
                    _password = value;

                    if (!String.IsNullOrWhiteSpace(_password) && _password.Length < 4)
                        UpdateRectangles(_password.Length);
                    else
                        UpdateRectangles(0);
                }
            }
        }

        private void Button_Clicked(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (button != null)
            {
                var str = (String)button.Tag;

                if (str == "-1")
                {
                    if (String.IsNullOrWhiteSpace(Password))
                        return;

                    Password = Password.Substring(0, Password.Length - 1);
                }
                else if (String.IsNullOrWhiteSpace(Password) || Password.Length < 4)
                {
                    Password = Password + str;

                    if (Password.Length == 4 && PasswordEntered != null)
                        PasswordEntered(this, null);
                }
            }
        }

        void UpdateRectangles(int length)
        {
            switch (length)
            {
                case 0:
                    rec1.Fill = UI_Utils.Instance.Transparent;
                    rec2.Fill = UI_Utils.Instance.Transparent;
                    rec3.Fill = UI_Utils.Instance.Transparent;
                    rec4.Fill = UI_Utils.Instance.Transparent;
                    break;
                case 1:
                    rec1.Fill = UI_Utils.Instance.White;
                    rec2.Fill = UI_Utils.Instance.Transparent;
                    rec3.Fill = UI_Utils.Instance.Transparent;
                    rec4.Fill = UI_Utils.Instance.Transparent;
                    break;
                case 2:
                    rec1.Fill = UI_Utils.Instance.White;
                    rec2.Fill = UI_Utils.Instance.White;
                    rec3.Fill = UI_Utils.Instance.Transparent;
                    rec4.Fill = UI_Utils.Instance.Transparent;
                    break;
                case 3:
                    rec1.Fill = UI_Utils.Instance.White;
                    rec2.Fill = UI_Utils.Instance.White;
                    rec3.Fill = UI_Utils.Instance.White;
                    rec4.Fill = UI_Utils.Instance.Transparent;
                    break;
                default:
                    break;
            }
        }

        public event EventHandler<EventArgs> PasswordEntered;
        public event EventHandler<EventArgs> PasswordOverlayVisibilityChanged;
    }
}
