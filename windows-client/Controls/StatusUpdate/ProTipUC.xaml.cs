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
        public ProTipUC(ProTip proTip, EventHandler<System.Windows.Input.GestureEventArgs> imageTap, EventHandler<System.Windows.Input.GestureEventArgs> dismissTap)
            : base(string.Empty, null, string.Empty, string.Empty)
        {
            InitializeComponent();
            if (!String.IsNullOrEmpty(proTip._header))
            {
                proTipTitleText.Visibility = Visibility.Visible;
                proTipTitleText.Text = proTip._header;
            }

            if (!String.IsNullOrEmpty(proTip._body))
            {
                proTipContentText.Visibility = Visibility.Visible;
                proTipContentText.Text = proTip._body;
            }

            if (!String.IsNullOrEmpty(proTip.ImageUrl))
            {
                Binding myBinding = new Binding();
                myBinding.Source = proTip.TipImage;
                proTipImage.SetBinding(Image.SourceProperty, myBinding);
                proTipImage.Visibility = Visibility.Visible;

                proTipImage.Tap += imageTap;
            }

            dismissButton.Tap += dismissTap;
        }
    }
}
