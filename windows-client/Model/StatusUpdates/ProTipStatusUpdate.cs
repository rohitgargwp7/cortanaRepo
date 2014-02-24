using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using windows_client.utils;

namespace windows_client.Model
{
    public class ProTipStatusUpdate : BaseStatusUpdate
    {
        public ProTipStatusUpdate()
            : base(string.Empty, null, string.Empty, string.Empty)
        {
            if (!String.IsNullOrEmpty(ProTipHelper.CurrentProTip._header))
            {
                ProTipTextVisibility = Visibility.Visible;
                Text = ProTipHelper.CurrentProTip._header;
            }

            if (!String.IsNullOrEmpty(ProTipHelper.CurrentProTip._body))
            {
                ProTipContentVisibility = Visibility.Visible;
                Content = ProTipHelper.CurrentProTip._body;
            }

            if (!String.IsNullOrEmpty(ProTipHelper.CurrentProTip.ImageUrl))
            {
                ProTipImage = ProTipHelper.CurrentProTip.TipImage;
                ProTipImageVisibility = Visibility.Visible;
            }
        }

        Visibility _proTipImageVisibility = Visibility.Collapsed;
        public Visibility ProTipImageVisibility
        {
            get
            {
                return _proTipImageVisibility;
            }
            set
            {
                if (value != _proTipImageVisibility)
                {
                    _proTipImageVisibility = value;
                    NotifyPropertyChanged("ProTipImageVisibility");
                }
            }
        }

        Visibility _proTipTextVisibility = Visibility.Collapsed;
        public Visibility ProTipTextVisibility
        {
            get
            {
                return _proTipTextVisibility;
            }
            set
            {
                if (value != _proTipTextVisibility)
                {
                    _proTipTextVisibility = value;
                    NotifyPropertyChanged("ProTipTextVisibility");
                }
            }
        }

        Visibility _proTipContentVisibility = Visibility.Collapsed;
        public Visibility ProTipContentVisibility
        {
            get
            {
                return _proTipContentVisibility;
            }
            set
            {
                if (value != _proTipContentVisibility)
                {
                    _proTipContentVisibility = value;
                    NotifyPropertyChanged("ProTipContentVisibility");
                }
            }
        }

        private string _content;
        public string Content
        {
            get
            {
                return _content;
            }
            set
            {
                if (value != _content)
                {
                    _content = value;
                    NotifyPropertyChanged("Content");
                }
            }
        }

        ImageSource _proTipImage = null;
        public ImageSource ProTipImage
        {
            get
            {
                return _proTipImage;
            }
            set
            {
                if (value != _proTipImage)
                {
                    _proTipImage = value;
                    NotifyPropertyChanged("ProTipImage");
                }
            }
        }
    }
}
