﻿#pragma checksum "C:\Users\milan\Desktop\windows-hike-client\windows-client\View\EnterName.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "8661CD0F84E55073507E0E3F90699C11"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.17929
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace windows_client {
    
    
    public partial class EnterName : Microsoft.Phone.Controls.PhoneApplicationPage {
        
        internal Microsoft.Phone.Controls.PhoneApplicationPage enterName;
        
        internal Microsoft.Phone.Shell.ProgressIndicator shellProgress;
        
        internal System.Windows.Controls.Grid LayoutRoot;
        
        internal System.Windows.Controls.Grid ContentPanel;
        
        internal System.Windows.Controls.TextBlock txtBlckPhoneNumber;
        
        internal System.Windows.Controls.Image avatarImage;
        
        internal Microsoft.Phone.Controls.PhoneTextBox txtBxEnterName;
        
        internal System.Windows.Controls.TextBlock nameErrorTxt;
        
        internal System.Windows.Controls.TextBlock feelingLazyTxtBlk;
        
        internal System.Windows.Controls.Image fbImage;
        
        internal System.Windows.Controls.TextBlock fbConnectTxtBlk;
        
        internal System.Windows.Controls.TextBlock msgTxtBlk;
        
        internal System.Windows.Controls.ProgressBar progressBar;
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Windows.Application.LoadComponent(this, new System.Uri("/windows-client;component/View/EnterName.xaml", System.UriKind.Relative));
            this.enterName = ((Microsoft.Phone.Controls.PhoneApplicationPage)(this.FindName("enterName")));
            this.shellProgress = ((Microsoft.Phone.Shell.ProgressIndicator)(this.FindName("shellProgress")));
            this.LayoutRoot = ((System.Windows.Controls.Grid)(this.FindName("LayoutRoot")));
            this.ContentPanel = ((System.Windows.Controls.Grid)(this.FindName("ContentPanel")));
            this.txtBlckPhoneNumber = ((System.Windows.Controls.TextBlock)(this.FindName("txtBlckPhoneNumber")));
            this.avatarImage = ((System.Windows.Controls.Image)(this.FindName("avatarImage")));
            this.txtBxEnterName = ((Microsoft.Phone.Controls.PhoneTextBox)(this.FindName("txtBxEnterName")));
            this.nameErrorTxt = ((System.Windows.Controls.TextBlock)(this.FindName("nameErrorTxt")));
            this.feelingLazyTxtBlk = ((System.Windows.Controls.TextBlock)(this.FindName("feelingLazyTxtBlk")));
            this.fbImage = ((System.Windows.Controls.Image)(this.FindName("fbImage")));
            this.fbConnectTxtBlk = ((System.Windows.Controls.TextBlock)(this.FindName("fbConnectTxtBlk")));
            this.msgTxtBlk = ((System.Windows.Controls.TextBlock)(this.FindName("msgTxtBlk")));
            this.progressBar = ((System.Windows.Controls.ProgressBar)(this.FindName("progressBar")));
        }
    }
}

