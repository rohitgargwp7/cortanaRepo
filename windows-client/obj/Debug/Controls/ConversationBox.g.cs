﻿#pragma checksum "C:\Users\milan\Desktop\windows-hike-client\windows-client\Controls\ConversationBox.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "82219DDD9559781A385D3117866FB0A1"
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


namespace windows_client.Controls {
    
    
    public partial class ConversationBox : System.Windows.Controls.UserControl {
        
        internal System.Windows.Controls.Grid LayoutRoot;
        
        internal System.Windows.Controls.Image profileImage;
        
        internal System.Windows.Controls.TextBlock userNameTxtBlck;
        
        internal System.Windows.Controls.TextBlock timestampTxtBlck;
        
        internal System.Windows.Controls.Image sdrImage;
        
        internal System.Windows.Shapes.Ellipse unreadCircle;
        
        internal System.Windows.Controls.RichTextBox lastMessageTxtBlck;
        
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
            System.Windows.Application.LoadComponent(this, new System.Uri("/windows-client;component/Controls/ConversationBox.xaml", System.UriKind.Relative));
            this.LayoutRoot = ((System.Windows.Controls.Grid)(this.FindName("LayoutRoot")));
            this.profileImage = ((System.Windows.Controls.Image)(this.FindName("profileImage")));
            this.userNameTxtBlck = ((System.Windows.Controls.TextBlock)(this.FindName("userNameTxtBlck")));
            this.timestampTxtBlck = ((System.Windows.Controls.TextBlock)(this.FindName("timestampTxtBlck")));
            this.sdrImage = ((System.Windows.Controls.Image)(this.FindName("sdrImage")));
            this.unreadCircle = ((System.Windows.Shapes.Ellipse)(this.FindName("unreadCircle")));
            this.lastMessageTxtBlck = ((System.Windows.Controls.RichTextBox)(this.FindName("lastMessageTxtBlck")));
        }
    }
}

