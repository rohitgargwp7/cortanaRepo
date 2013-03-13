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
        private string _metaDataString;
        public Attachment FileAttachment;

        public string Text;
        public string TimeStamp;
        public List<MyChatBubble> splitChatBubbles = null;

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

        public string MetaDataString
        {
            get { return _metaDataString; }
            set { _metaDataString = value; }
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
            this._metaDataString = cm.MetaDataString;
            if (cm.FileAttachment != null)
            {
                this.FileAttachment = cm.FileAttachment;
                setAttachmentState(cm.FileAttachment.FileState);
            }
            else if (!cm.HasAttachment)
            {
                var currentPage = ((App)Application.Current).RootFrame.Content as NewChatThread;
                if (currentPage != null)
                {
                    ContextMenu contextMenu = null;
                    if (String.IsNullOrEmpty(cm.MetaDataString) || !cm.MetaDataString.Contains("poke"))
                    {
                        contextMenu = currentPage.createAttachmentContextMenu(Attachment.AttachmentState.COMPLETED,
                            false, false); //since it is not an attachment message this bool won't make difference
                    }
                    else
                    {
                        contextMenu = currentPage.createAttachmentContextMenu(Attachment.AttachmentState.CANCELED,
                            true, true); //set to tru to have only delete option for nudge bubbles
                    }
                    ContextMenuService.SetContextMenu(this, contextMenu);
                }
            }
        }

        public void setTapEvent(EventHandler<Microsoft.Phone.Controls.GestureEventArgs> tapEventHandler)
        {
            var gl = GestureService.GetGestureListener(this);
            gl.Tap += tapEventHandler;
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
