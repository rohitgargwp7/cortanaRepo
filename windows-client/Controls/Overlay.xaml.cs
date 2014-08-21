using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.ComponentModel;
using windows_client.Languages;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Info;
using System.Windows.Media.Imaging;

namespace windows_client.Controls
{
    /// <summary>
    /// Page elements tapping should be controlled by page itself
    /// </summary>
    public partial class Overlay : UserControl
    {
        // Use this from XAML to control whether animation is on or off
        #region EnableAnimation Dependency Property

        public static readonly DependencyProperty EnableAnimationProperty =
            DependencyProperty.Register("EnableAnimation", typeof(bool), typeof(Overlay), new PropertyMetadata(true, null));

        public static void SetEnableAnimation(Overlay element, bool value)
        {
            element.SetValue(EnableAnimationProperty, value);
        }

        public static bool GetEnableAnimation(Overlay element)
        {
            return (bool)element.GetValue(EnableAnimationProperty);
        }

        #endregion

        // Use this for MVVM binding IsVisible
        #region IsVisible Dependency Property

        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.Register("IsVisible", typeof(bool), typeof(Overlay), new PropertyMetadata(OnIsVisiblePropertyChanged));

        private static void OnIsVisiblePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            Overlay overlay = obj as Overlay;
            bool visibility = (bool)e.NewValue;

            if (visibility)
            {
                if (Overlay.GetEnableAnimation(overlay))
                    overlay.showContent.Begin();
                //overlay._rootFrame.ApplicationBar.IsEnabled = false;
            }
        }

        public static void SetIsVisible(Overlay element, bool value)
        {
            element.SetValue(IsVisibleProperty, value);
        }

        public static bool GetIsVisible(Overlay element)
        {
            return (bool)element.GetValue(IsVisibleProperty);
        }

        #endregion

        #region Dependency properties to be set from view

        public String Title
        {
            get
            {
                return (String)GetValue(TitleProperty);
            }
            set
            {
                SetValue(TitleProperty, value);
            }
        }

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            "Title", typeof(String), typeof(Overlay), new PropertyMetadata(OnTitlePropertyChanged));

        private static void OnTitlePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            Overlay overlay = obj as Overlay;
            overlay.title.Text = e.NewValue as String;
            overlay.title.Visibility = String.IsNullOrEmpty(e.NewValue as String) ? Visibility.Collapsed : Visibility.Visible;
        }

        public String LeftButtonContent
        {
            get
            {
                return (String)GetValue(LeftButtonContentProperty);
            }
            set
            {
                SetValue(LeftButtonContentProperty, value);
            }
        }

        public static readonly DependencyProperty LeftButtonContentProperty = DependencyProperty.Register(
            "LeftButtonContent", typeof(String), typeof(Overlay), new PropertyMetadata(OnLeftButtonContentPropertyChanged));

        private static void OnLeftButtonContentPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            Overlay overlay = obj as Overlay;
            overlay.yesButton.Content = e.NewValue as String;
            overlay.yesButton.Visibility = String.IsNullOrEmpty(e.NewValue as String) ? Visibility.Collapsed : Visibility.Visible;
        }

        public String RightButtonContent
        {
            get
            {
                return (String)GetValue(RightButtonContentProperty);
            }
            set
            {
                SetValue(RightButtonContentProperty, value);
            }
        }

        public static readonly DependencyProperty RightButtonContentProperty = DependencyProperty.Register(
            "RightButtonContent", typeof(String), typeof(Overlay), new PropertyMetadata(OnRightButtonContentPropertyChanged));

        private static void OnRightButtonContentPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            Overlay overlay = obj as Overlay;
            overlay.noButton.Content = e.NewValue as String;
            overlay.noButton.Visibility = String.IsNullOrEmpty(e.NewValue as String) ? Visibility.Collapsed : Visibility.Visible;

            if (overlay.yesButton.Visibility == Visibility.Visible)
            {
                if (overlay.noButton.Visibility == Visibility.Visible)
                {
                    overlay.yesButton.SetValue(Grid.ColumnSpanProperty, 1);
                    overlay.noButton.SetValue(Grid.ColumnSpanProperty, 1);
                    overlay.noButton.SetValue(Grid.ColumnProperty, 1);
                }
                else
                {
                    overlay.yesButton.SetValue(Grid.ColumnSpanProperty, 2);
                }
            }
            else
            {
                if (overlay.noButton.Visibility == Visibility.Visible)
                {
                    overlay.noButton.SetValue(Grid.ColumnSpanProperty, 2);
                    overlay.noButton.SetValue(Grid.ColumnProperty, 0);
                }
            }
        }

        public BitmapImage DisplayImage
        {
            get
            {
                return (BitmapImage)GetValue(DisplayImageProperty);
            }
            set
            {
                SetValue(DisplayImageProperty, value);
            }
        }

        public static readonly DependencyProperty DisplayImageProperty = DependencyProperty.Register(
            "DisplayImage", typeof(BitmapImage), typeof(Overlay), new PropertyMetadata(OnDisplayImagePropertyChanged));

        private static void OnDisplayImagePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            Overlay overlay = obj as Overlay;
            try
            {
                overlay.image.Source = e.NewValue as BitmapImage;
                overlay.image.Visibility = Visibility.Visible;
            }
            catch
            {
                overlay.image.Visibility = Visibility.Collapsed;
            }
        }

        public String Message
        {
            get
            {
                return (String)GetValue(MessageProperty);
            }
            set
            {
                SetValue(MessageProperty, value);
            }
        }

        public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
            "Message", typeof(String), typeof(Overlay), new PropertyMetadata(OnMessagePropertyChanged));

        private static void OnMessagePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            Overlay overlay = obj as Overlay;
            overlay.message.Text = e.NewValue as String;
            overlay.message.Visibility = String.IsNullOrEmpty(e.NewValue as String) ? Visibility.Collapsed : Visibility.Visible;
        }

        #endregion

        #region Events

        public event EventHandler<EventArgs> VisibilityChanged;
        public event EventHandler<EventArgs> LeftClicked;
        public event EventHandler<EventArgs> RightClicked;

        #endregion

        private PhoneApplicationFrame _rootFrame = null;

        public Overlay()
        {
            InitializeComponent();
            IsEnabled = true;

            Loaded += Overlay_Loaded;
            hideContent.Completed += hideContent_Completed;
        }

        void hideContent_Completed(object sender, EventArgs e)
        {
            this.SetVisibility(false);
        }

        private void Overlay_Loaded(object sender, RoutedEventArgs e)
        {
            this.AttachBackKeyPressed();
            if (Overlay.GetEnableAnimation(this))
            {
                this.LayoutRoot.Opacity = 0;
                this.xProjection.RotationX = 90;
            }
        }

        private void AttachBackKeyPressed()
        {
            // Detect back pressed
            if (this._rootFrame == null)
            {
                this._rootFrame = Application.Current.RootVisual as PhoneApplicationFrame;
                this._rootFrame.BackKeyPress += Overlay_BackKeyPress;
            }
        }

        private void Overlay_BackKeyPress(object sender, CancelEventArgs e)
        {
            // If back is pressed whilst open, close and cancel back to stop app exiting
            if (this.Visibility == System.Windows.Visibility.Visible)
            {
                this.BeginHideAnimation();
                e.Cancel = true;
            }
        }

        bool noClicked = false;
        private void noButton_Click(object sender, RoutedEventArgs e)
        {
            if (noClicked || yesClicked)
                return;

            noClicked = true;

            this.BeginHideAnimation();

            if (RightClicked != null)
                RightClicked(sender, e);
        }

        bool yesClicked = false;
        private void yesButton_Click(object sender, RoutedEventArgs e)
        {
            if (noClicked || yesClicked)
                return;

            yesClicked = true;

            this.BeginHideAnimation();

            if (LeftClicked != null)
                LeftClicked(sender, e);
        }

        private void BeginHideAnimation()
        {
            if (Overlay.GetEnableAnimation(this))
                this.hideContent.Begin();
            else
                this.SetVisibility(false);
        }

        public void SetVisibility(bool visible)
        {
            if (visible)
            {
                Overlay.SetIsVisible(this, true);
                this.Visibility = System.Windows.Visibility.Visible;

                if (this.VisibilityChanged != null)
                    this.VisibilityChanged(this, new EventArgs());
            }
            else
            {
                Overlay.SetIsVisible(this, false);
                this.Visibility = System.Windows.Visibility.Collapsed;

                yesClicked = false;
                noClicked = false;

                if (this.VisibilityChanged != null)
                    this.VisibilityChanged(this, null);
            }
        }
    }
}
