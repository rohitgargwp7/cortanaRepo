using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media.Imaging;
using windows_client.utils;
using System.Windows.Data;

namespace windows_client.Controls.StatusUpdate
{
    public partial class ProTipUC : StatusUpdateBox
    {
        public ProTipUC(EventHandler<System.Windows.Input.GestureEventArgs> imageTap, EventHandler<System.Windows.Input.GestureEventArgs> dismissTap)
            : base(string.Empty, null, string.Empty, string.Empty)
        {
            InitializeComponent();
            if (!String.IsNullOrEmpty(ProTipHelper.CurrentProTip.Header))
            {
                proTipTitleText.Visibility = Visibility.Visible;
                proTipTitleText.Text = ProTipHelper.CurrentProTip.Header;
            }

            if (!String.IsNullOrEmpty(ProTipHelper.CurrentProTip.Body))
            {
                proTipContentText.Visibility = Visibility.Visible;
                proTipContentText.Text = ProTipHelper.CurrentProTip.Body;
            }

            if (!String.IsNullOrEmpty(ProTipHelper.CurrentProTip.ImageUrl))
            {
                Binding myBinding = new Binding();
                myBinding.Source = ProTipHelper.CurrentProTip.TipImage;
                proTipImage.SetBinding(Image.SourceProperty, myBinding);
                proTipImage.Visibility = Visibility.Visible;

                proTipImage.Tap += imageTap;
            }

            dismissButton.Tap += dismissTap;
        }
    }
}
