using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Phone.Shell;
using System.Windows.Controls.Primitives;
using Microsoft.Phone.Controls;

namespace Phone.Controls
{
    public class MyProgressIndicator : ContentControl
    {       
        private System.Windows.Shapes.Rectangle backgroundRect;
        private System.Windows.Controls.StackPanel stackPanel;
        private System.Windows.Controls.ProgressBar progressBar;
        private System.Windows.Controls.TextBlock textBlockStatus;

        private bool currentSystemTrayState;       
        private static string defaultText = "Loading...";
        private bool showLabel;
        private string labelText;
        
        
        public MyProgressIndicator(string text)
        {
            this.DefaultStyleKey = typeof(MyProgressIndicator);
            defaultText = text;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.backgroundRect = this.GetTemplateChild("backgroundRect") as Rectangle;
            this.stackPanel = this.GetTemplateChild("stackPanel") as StackPanel;
            this.progressBar = this.GetTemplateChild("progressBar") as ProgressBar;
            this.textBlockStatus = this.GetTemplateChild("textBlockStatus") as TextBlock;

            this.Text = labelText;
            
            InitializeProgressType();
        }

        internal Popup ChildWindowPopup
        {
            get;
            private set;
        }

        private static PhoneApplicationFrame RootVisual
        {
            get
            {
                return Application.Current == null ? null : Application.Current.RootVisual as PhoneApplicationFrame;
            }
        }

        public bool ShowLabel
        {
            get
            {
                return this.showLabel;
            }
            set
            {
                this.showLabel = value;
            }
        }

        public string Text
        {
            get
            {
                return labelText;                
            }
            set
            {
                this.labelText = value;
                if (this.textBlockStatus != null)
                {
                    this.textBlockStatus.Text = value;
                }
            }
        }

        public ProgressBar ProgressBar
        {
            get
            {
                return this.progressBar;
            }
        }

        public new double Opacity
        {
            get
            {
                return this.backgroundRect.Opacity;
            }
            set
            {
                this.backgroundRect.Opacity = value;
            }
        }

        public void Hide()
        {
            // Restore system tray
            SystemTray.IsVisible = currentSystemTrayState;
            this.progressBar.IsIndeterminate = false;
            this.ChildWindowPopup.IsOpen = false;

        }

        public void Show()
        {
            if (this.ChildWindowPopup == null)
            {
                this.ChildWindowPopup = new Popup();

                try
                {
                    this.ChildWindowPopup.Child = this;
                }
                catch (ArgumentException)
                {
                    throw new InvalidOperationException("The control is already shown.");
                }
            }


            if (this.ChildWindowPopup != null && Application.Current.RootVisual != null)
            {
                // Configure accordingly to the type
                InitializeProgressType();

                // Show popup
                this.ChildWindowPopup.IsOpen = true;
            }
        }
      

        private void HideSystemTray()
        {
            // Capture current state of the system tray
            this.currentSystemTrayState = SystemTray.IsVisible;
            // Hide it
            SystemTray.IsVisible = false;
        }

        private void InitializeProgressType()
        {
            this.HideSystemTray();
            if (this.progressBar == null)
                return;

            this.progressBar.Value = 0;
            this.Opacity = 0.7;
            this.backgroundRect.Visibility = System.Windows.Visibility.Visible;
            this.stackPanel.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            this.progressBar.Foreground = (Brush)Application.Current.Resources["PhoneForegroundBrush"];
            this.textBlockStatus.Text = defaultText;
            this.textBlockStatus.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            this.textBlockStatus.Visibility = System.Windows.Visibility.Visible;
            this.textBlockStatus.Margin = new Thickness();
            this.Height = 800;
            this.progressBar.IsIndeterminate = true;
        }

    }
}
