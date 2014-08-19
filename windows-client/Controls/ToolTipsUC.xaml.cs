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
            this.Visibility = Visibility.Collapsed;
            leftIcon.Visibility = Visibility.Collapsed;
            rightIcon.Visibility = Visibility.Collapsed;
        }

        #region dependency property region

        public static readonly DependencyProperty IsShowProperty =
            DependencyProperty.Register("IsShow", typeof(Boolean), typeof(ToolTipsUC), new PropertyMetadata(OnIsShowPropertyChanged));

        public static void OnIsShowPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ToolTipsUC TempToolTip = obj as ToolTipsUC;
            TempToolTip.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
        }

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

        public static readonly DependencyProperty ControlBackgroundColorProperty =
            DependencyProperty.Register("ControlBackgroundColor", typeof(Brush), typeof(ToolTipsUC), new PropertyMetadata(OnBackGroundColorChanged));

        public static void OnBackGroundColorChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ToolTipsUC TempToolTip = obj as ToolTipsUC;
            Brush TempBg = (Brush)e.NewValue;
            TempToolTip.toolTipGrid.Background = TempBg;
        }

        public Brush ControlBackgroundColor
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
            tempToolTip.leftIcon.Visibility = ((tempSource != null) ? (Visibility.Visible) : (Visibility.Collapsed));

            var margin = tempToolTip.TextGrid.Margin;
            margin.Left = tempSource == null ? 24 : 0;
            tempToolTip.TextGrid.Margin = margin;
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
            ToolTipsUC tempToolTip = obj as ToolTipsUC;
            ImageSource tempSource = (ImageSource)e.NewValue;
            tempToolTip.rightIcon.Source = tempSource;
            tempToolTip.rightIcon.Visibility = ((tempSource != null) ? (Visibility.Visible) : (Visibility.Collapsed));

            var margin = tempToolTip.TextGrid.Margin;
            margin.Right = tempSource == null ? 24 : 0;
            tempToolTip.TextGrid.Margin = margin;
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

        public static readonly DependencyProperty TipHeadTextProperty =
    DependencyProperty.Register("TipHeadText", typeof(String), typeof(ToolTipsUC), new PropertyMetadata(OnTipHeadTextChanged));

        public static void OnTipHeadTextChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ToolTipsUC tempToolTip = obj as ToolTipsUC;
            String tempText = (String)e.NewValue;
            tempToolTip.tipHeadText.Text = tempText;
            tempToolTip.tipHeadText.Visibility = String.IsNullOrEmpty(tempText) ? Visibility.Collapsed : Visibility.Visible;
        }

        public String TipHeadText
        {
            get
            {
                return (String)GetValue(TipHeadTextProperty);
            }
            set
            {
                SetValue(TipHeadTextProperty, value);
            }
        }

        #endregion

        #region events

        public event EventHandler<EventArgs> LeftIconClicked;
        public event EventHandler<EventArgs> RightIconClicked;
        public event EventHandler<EventArgs> FullTipTapped;

        #endregion

        public void ResetToolTip()
        {
            LeftIconClicked = null;
            RightIconClicked = null;
            FullTipTapped = null;
            LeftIconSource = null;
            RightIconSource = null;
            TipHeadText = null;
            TipText = null;
        }

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
            if (FullTipTapped != null)
                FullTipTapped(sender, e);
        }
    }
}
