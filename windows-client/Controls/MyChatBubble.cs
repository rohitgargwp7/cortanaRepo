using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Navigation;
using windows_client.Model;
using windows_client.utils;
using System.Collections.Generic;

namespace windows_client.Controls
{
    public class MyChatBubble : UserControl
    {
        private long _timeStampLong;
        private long _messageId;
        private ConvMessage.State _messageState;

        public static DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(MyChatBubble), new PropertyMetadata(""));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static DependencyProperty TimeStampProperty = DependencyProperty.Register("TimeStamp", typeof(string), typeof(MyChatBubble), new PropertyMetadata(""));

        public string TimeStamp
        {
            get { return (string)GetValue(TimeStampProperty); }
            set
            {
                SetValue(TimeStampProperty, value);
            }
        }

        //TODO: Try to use a single property for timestamp.
        //either dispose off the convmessage or else keep a reference to it in this class
        public long TimeStampLong
        {
            get
            {
                return _timeStampLong;
            }
            set
            {
                _timeStampLong = value;
            }
        }

        public long MessageId
        {
            get
            {
                return _messageId;
            }
            set
            {
                _messageId = value;
            }
        }

        public ConvMessage.State MessageStatus
        {
            get
            {
                return _messageState;
            }
            set
            {
                _messageState = value;
            }
        }

        public MyChatBubble()
        {
        }

        public MyChatBubble(long messageId)
        {
            this.MessageId = messageId;
        }

        public MyChatBubble(ConvMessage cm, Dictionary<string, RoutedEventHandler> contextMenuDictionary)
        {
            this.Text = cm.Message;
            this.TimeStamp = TimeUtils.getTimeStringForChatThread(cm.Timestamp);
            this._messageId = cm.MessageId;
            this._timeStampLong = cm.Timestamp;
            this._messageState = cm.MessageStatus;
            
            ContextMenu menu = new ContextMenu();
            menu.IsZoomEnabled = false;

            foreach (KeyValuePair<string, RoutedEventHandler> entry in contextMenuDictionary)
            {
                MenuItem menuItem = new MenuItem();
                menuItem.Header = entry.Key;
                menuItem.Click += entry.Value;
                menu.Items.Add(menuItem);
            }
            ContextMenuService.SetContextMenu(this, menu);
        }

        public void setTapEvent(EventHandler<Microsoft.Phone.Controls.GestureEventArgs> tapEventHandler)
        {
            var gl = GestureService.GetGestureListener(this);
            gl.Tap += tapEventHandler;
        }

    }
}
