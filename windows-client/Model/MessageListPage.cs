﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Data.Linq;

namespace windows_client.Model
{
    public class MessageListPage
    {
        #region member variables

        private string _msisdn;
        private string _contactName;
        private string _lastMessage;
        private string _timeStamp;
        private bool _isOnhike;
        /*private Image _avatar;*/

        #endregion

        #region Properties

        public string ContactName
        {
            get
            {
                return _contactName;
            }
            set
            {
                if (_contactName != value)
                {
                    _contactName = value;
                }
            }
        }


        public string LastMessage
        {
            get
            {
                return _lastMessage;
            }
            set
            {
                if (_lastMessage != value)
                {

                    _lastMessage = value;

                }
            }
        }


        public string TimeStamp
        {
            get
            {
                return _timeStamp;
            }
            set
            {
                if (_timeStamp != value)
                {
                   
                    _timeStamp = value;
                
                }
            }
        }

        public string MSISDN
        {
            get
            {
                return _msisdn;
            }
            set
            {
                if (_msisdn != value)
                {
                    _msisdn = value;
                }
            }
        }

        public bool IsOnhike
        {
            get
            {
                return _isOnhike;
            }
            set
            {
                if (_isOnhike != value)
                {
                   
                    _isOnhike = value;
                  
                }
            }
        }

        public MessageListPage(string msisdn, string contactName, string lastMessage, bool isOnhike, string relativeTime)
        {
            this._msisdn = msisdn;
            this._contactName = contactName;
            this._lastMessage = lastMessage;
            this._timeStamp = relativeTime;
            this._isOnhike = isOnhike;
        }

        public MessageListPage(string msisdn, string contactName, string lastMessage, string relativeTime)
            : this(msisdn, contactName, lastMessage, false, relativeTime)
        {

        }

        public MessageListPage()
        {
            _msisdn = null;
            _contactName = null;
            _lastMessage = null;
            _timeStamp = null;
            _isOnhike = false;
        }
        /*
       public Image Avatar
       {
           get
           {
               return _avatar;
           }
           set
           {
               if (_avatar != value)
               {
                   NotifyPropertyChanging("Avatar");
                   _avatar = value;
                   NotifyPropertyChanged("Avatar");
               }
           }
       }
       */

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            MessageListPage o = obj as MessageListPage;

            if ((System.Object)o == null)
            {
                return false;
            }
            return (_msisdn == o.MSISDN);
        }
        #endregion


    }
}
