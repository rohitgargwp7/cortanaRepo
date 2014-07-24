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
    public partial class ToolTipsUC : UserControl
    {
        public ToolTipsUC()
        {
            InitializeComponent();
            leftIcon.Visibility = Visibility.Collapsed;
            rightIcon.Visibility = Visibility.Collapsed;
        }
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        #region dependency property region

        public static readonly DependencyProperty ControlBackgroundColorProperty =
            DependencyProperty.Register("ControlBackgroundColor", typeof(Brush), typeof(ToolTipsUC), new PropertyMetadata(OnBackGroundColorChanged));

        public static void OnBackGroundColorChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ToolTipsUC TempToolTip = obj as ToolTipsUC;
            Brush TempBg = (Brush)e.NewValue;
            TempToolTip.toolTipGrid.Background = TempBg;
        }

        public Brush ControlBackGroundColor
        {
            get
            {
                return (Brush)GetValue(ControlBackgroundColorProperty);
            }
            set
            {
                SetValue(ControlBackgroundColorProperty, value);
            }
        }

        public static readonly DependencyProperty LeftIconSourceProperty =
            DependencyProperty.Register("LeftIconSource", typeof(ImageSource), typeof(ToolTipsUC), new PropertyMetadata(OnleftIconSourceChanged));

        public static void OnleftIconSourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ToolTipsUC tempToolTip = obj as ToolTipsUC;
            ImageSource tempSource = (ImageSource)e.NewValue;
            tempToolTip.leftIcon.Source = tempSource;
            if (tempSource != null)
            {
                tempToolTip.leftIcon.Visibility = Visibility.Visible;
            }
            else
            {
                tempToolTip.leftIcon.Visibility = Visibility.Collapsed;
            }
        }

        public ImageSource LeftIconSource
        {
            get
            {
                return (ImageSource)GetValue(LeftIconSourceProperty);
            }
            set
            {
                SetValue(LeftIconSourceProperty, value);
            }
        }

        public static readonly DependencyProperty RightIconSourceProperty =
            DependencyProperty.Register("RightIconSource", typeof(ImageSource), typeof(ToolTipsUC), new PropertyMetadata(OnRightIconSourceChanged));

        public static void OnRightIconSourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ToolTipsUC TempToolTip = obj as ToolTipsUC;
            ImageSource TempSource = (ImageSource)e.NewValue;
            TempToolTip.rightIcon.Source = TempSource;
            if (TempSource != null)
            {
                TempToolTip.rightIcon.Visibility = Visibility.Visible;
            }
            else
            {
                TempToolTip.rightIcon.Visibility = Visibility.Collapsed;
            }
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

        public static readonly DependencyProperty TipTextProperty =
            DependencyProperty.Register("TipText", typeof(String), typeof(ToolTipsUC), new PropertyMetadata(OnTipTextChanged));

        public static void OnTipTextChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ToolTipsUC tempToolTip = obj as ToolTipsUC;
            String tempText = (String)e.NewValue;
            tempToolTip.tipTextbox.Text = tempText;
        }

        public String TipText
        {
            get
            {
                return (String)GetValue(TipTextProperty);
            }
            set
            {
                SetValue(TipTextProperty, value);
            }
        }

        #endregion

        #region events

        public event EventHandler<EventArgs> LeftIconClicked;
        public event EventHandler<EventArgs> RightIconClicked;
        public event EventHandler<EventArgs> ControlClicked;

        #endregion

        public void leftIcon_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (LeftIconClicked != null)
                LeftIconClicked(sender, e);
        }

        public void rightIcon_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (RightIconClicked != null)
                RightIconClicked(sender, e);
        }

        public void toolTipGrid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (ControlClicked != null)
                ControlClicked(sender, e);
        }
    }
}
