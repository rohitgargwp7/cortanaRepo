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
using windows_client.View;
using windows_client.DbUtils;

namespace windows_client.Controls
{
    public class MyChatBubble : UserControl
    {
        private long _timeStampLong;
        private long _messageId;
        private ConvMessage.State _messageState;
        public Attachment FileAttachment;

        public string Text;
        public string TimeStamp;

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

        public MyChatBubble(ConvMessage cm)
        {
            this.Text = cm.Message;
            this.TimeStamp = TimeUtils.getTimeStringForChatThread(cm.Timestamp);
            this._messageId = cm.MessageId;
            this._timeStampLong = cm.Timestamp;
            this._messageState = cm.MessageStatus;
            if (cm.FileAttachment != null)
            {
                this.FileAttachment = cm.FileAttachment;
                setAttachmentState(cm.FileAttachment.FileState);
            }
            else if (!cm.HasAttachment)
            {
                setNonAttachmentContextMenu();  
            }
        }

        public void setTapEvent(EventHandler<Microsoft.Phone.Controls.GestureEventArgs> tapEventHandler)
        {
            var gl = GestureService.GetGestureListener(this);
            gl.Tap += tapEventHandler;
        }

        protected void setContextMenu(Dictionary<string, EventHandler<Microsoft.Phone.Controls.GestureEventArgs>> contextMenuDictionary)
        {
            ContextMenu menu = new ContextMenu();
            menu.IsZoomEnabled = true;
            foreach (KeyValuePair<string, EventHandler<Microsoft.Phone.Controls.GestureEventArgs>> entry in contextMenuDictionary)
            {
                MenuItem menuItem = new MenuItem();
                menuItem.Header = entry.Key;
                var gl = GestureService.GetGestureListener(menuItem);
                gl.Tap += entry.Value;
                menu.Items.Add(menuItem);
            }
            ContextMenuService.SetContextMenu(this, menu);
        }


        private void setNonAttachmentContextMenu()
        { 
            var currentPage = ((App)Application.Current).RootFrame.Content as NewChatThread;
            if (currentPage != null)
            {
                setContextMenu(currentPage.NonAttachmentMenu);
            }
        }

        protected virtual void uploadOrDownloadCanceled()
        { }

        protected virtual void uploadOrDownloadStarted()
        { }

        protected virtual void uploadOrDownloadCompleted()
        { }


        public virtual void setAttachmentState(Attachment.AttachmentState attachmentState)
        {
        }
    }
}
