﻿#pragma checksum "C:\Users\milan\Desktop\windows-hike-client\windows-client\Controls\StatusUpdate\ImageStatusUpdate.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "1FBE12BE90810C8198346FD5D74C5137"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.17929
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

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
using windows_client.Controls.StatusUpdate;


namespace windows_client.Controls.StatusUpdate {
    
    
    public partial class ImageStatusUpdate : windows_client.Controls.StatusUpdate.StatusUpdateBox {
        
        internal System.Windows.Controls.Grid LayoutRoot;
        
        internal System.Windows.Controls.Image userProfileImage;
        
        internal System.Windows.Controls.TextBlock userNameTxtBlk;
        
        internal System.Windows.Controls.TextBlock statusTextTxtBlk;
        
        internal System.Windows.Controls.TextBlock timestampTxtBlk;
        
        internal System.Windows.Controls.Image statusImage;
        
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
            System.Windows.Application.LoadComponent(this, new System.Uri("/windows-client;component/Controls/StatusUpdate/ImageStatusUpdate.xaml", System.UriKind.Relative));
            this.LayoutRoot = ((System.Windows.Controls.Grid)(this.FindName("LayoutRoot")));
            this.userProfileImage = ((System.Windows.Controls.Image)(this.FindName("userProfileImage")));
            this.userNameTxtBlk = ((System.Windows.Controls.TextBlock)(this.FindName("userNameTxtBlk")));
            this.statusTextTxtBlk = ((System.Windows.Controls.TextBlock)(this.FindName("statusTextTxtBlk")));
            this.timestampTxtBlk = ((System.Windows.Controls.TextBlock)(this.FindName("timestampTxtBlk")));
            this.statusImage = ((System.Windows.Controls.Image)(this.FindName("statusImage")));
        }
    }
}

