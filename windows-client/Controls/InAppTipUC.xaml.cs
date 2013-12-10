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
using System.Windows.Media;

namespace windows_client.Controls
{
    public partial class InAppTipUC : UserControl
    {
        public InAppTipUC()
        {
            InitializeComponent();
        }

        private void LayoutRoot_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (Dismissed != null)
                Dismissed(this, null);
        }

        public Int32 TipIndex
        {
            get
            {
                return (Int32)GetValue(TipIndexProperty);
            }
            set
            {
                SetValue(TipIndexProperty, value);
            }
        }

        public static readonly DependencyProperty TipIndexProperty = DependencyProperty.Register(
            "TipIndex", typeof(Int32), typeof(InAppTipUC), null);

        public String Tip
        {
            get
            {
                return (String)GetValue(TipProperty);
            }
            set
            {
                SetValue(TipProperty, value);
            }
        }

        public static readonly DependencyProperty TipProperty = DependencyProperty.Register(
            "Tip", typeof(String), typeof(InAppTipUC), new PropertyMetadata(OnTipChanged));

        private static void OnTipChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            InAppTipUC tipControl = obj as InAppTipUC;
            tipControl.tipText.Text = (String)e.NewValue;
        }
        
        public Visibility TopPathVisibility
        {
            get
            {
                return (Visibility)GetValue(TopPathVisibilityProperty);
            }
            set
            {
                SetValue(TopPathVisibilityProperty, value);
            }
        }

        public static readonly DependencyProperty TopPathVisibilityProperty = DependencyProperty.Register(
            "TopPathVisibility", typeof(Visibility), typeof(InAppTipUC), new PropertyMetadata(OnTopPathVisibilityChanged));

        private static void OnTopPathVisibilityChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            InAppTipUC tipControl = obj as InAppTipUC;
            tipControl.topBubblePointer.Visibility = (Visibility)e.NewValue;
        }
        
        public Thickness TopPathMargin
        {
            get
            {
                return (Thickness)GetValue(TopPathMarginProperty);
            }
            set
            {
                SetValue(TopPathMarginProperty, value);
            }
        }

        public static readonly DependencyProperty TopPathMarginProperty = DependencyProperty.Register(
            "TopPathMargin", typeof(Thickness), typeof(InAppTipUC), new PropertyMetadata(OnTopPathMarginChanged));

        private static void OnTopPathMarginChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            InAppTipUC tipControl = obj as InAppTipUC;
            tipControl.topBubblePointer.Margin = (Thickness)e.NewValue;
        }

        public Visibility BottomPathVisibility
        {
            get
            {
                return (Visibility)GetValue(BottomPathVisibilityProperty);
            }
            set
            {
                SetValue(BottomPathVisibilityProperty, value);
            }
        }

        public static readonly DependencyProperty BottomPathVisibilityProperty = DependencyProperty.Register(
            "BottomPathVisibility", typeof(Visibility), typeof(InAppTipUC), new PropertyMetadata(OnBottomPathVisibilityChanged));

        private static void OnBottomPathVisibilityChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            InAppTipUC tipControl = obj as InAppTipUC;
            tipControl.bottomBubblePointer.Visibility = (Visibility)e.NewValue;
        }
        
        public Thickness BottomPathMargin
        {
            get
            {
                return (Thickness)GetValue(BottomPathMarginProperty);
            }
            set
            {
                SetValue(BottomPathMarginProperty, value);
            }
        }

        public static readonly DependencyProperty BottomPathMarginProperty = DependencyProperty.Register(
            "BottomPathMargin", typeof(Thickness), typeof(InAppTipUC), new PropertyMetadata(OnBottomPathMarginChanged));

        private static void OnBottomPathMarginChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            InAppTipUC tipControl = obj as InAppTipUC;
            tipControl.bottomBubblePointer.Margin = (Thickness)e.NewValue;
        }

        public SolidColorBrush TipBackground
        {
            get
            {
                return (SolidColorBrush)GetValue(TipBackgroundProperty);
            }
            set
            {
                SetValue(TipBackgroundProperty, value);
            }
        }

        public static readonly DependencyProperty TipBackgroundProperty = DependencyProperty.Register(
            "TipBackground", typeof(SolidColorBrush), typeof(InAppTipUC), new PropertyMetadata(OnTipBackgroundChanged));

        private static void OnTipBackgroundChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            InAppTipUC tipControl = obj as InAppTipUC;
            tipControl.topBubblePointer.Fill = (SolidColorBrush)e.NewValue;
            tipControl.bottomBubblePointer.Fill = (SolidColorBrush)e.NewValue;
            tipControl.tipBackground.Background = (SolidColorBrush)e.NewValue;
        }

        public SolidColorBrush TipForeground
        {
            get
            {
                return (SolidColorBrush)GetValue(TipForegroundProperty);
            }
            set
            {
                SetValue(TipForegroundProperty, value);
            }
        }

        public static readonly DependencyProperty TipForegroundProperty = DependencyProperty.Register(
            "TipForeground", typeof(SolidColorBrush), typeof(InAppTipUC), new PropertyMetadata(OnTipForegroundChanged));

        private static void OnTipForegroundChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            InAppTipUC tipControl = obj as InAppTipUC;
            tipControl.tipText.Foreground = (SolidColorBrush)e.NewValue;

            if ((SolidColorBrush)e.NewValue == UI_Utils.Instance.White)
                tipControl.closeButtonImage.Source = UI_Utils.Instance.CloseButtonWhiteImage;
            else
                tipControl.closeButtonImage.Source = UI_Utils.Instance.CloseButtonBlackImage;
        }

        public event EventHandler<EventArgs> Dismissed;

        private void close_Tapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (Dismissed != null)
                Dismissed(this, null);
        }
    }
}
