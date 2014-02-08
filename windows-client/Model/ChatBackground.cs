using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;
using System.IO.IsolatedStorage;
using System.IO;
using windows_client.Misc;
using windows_client.utils;
using System.Windows.Media.Imaging;
using System.Windows;
using System.ComponentModel;

namespace windows_client.Model
{
    public class ChatBackground : INotifyPropertyChanged
    {
        public SolidColorBrush BorderColor
        {
            get
            {
                return IsSelected ? UI_Utils.Instance.White : UI_Utils.Instance.Transparent;
            }
        }


        SolidColorBrush _backgroundColor;
        public SolidColorBrush BackgroundColor
        {
            get
            {
                if (_backgroundColor == null)
                    _backgroundColor = UI_Utils.Instance.ConvertStringToColor(Background);

                return _backgroundColor;
            }
        }

        SolidColorBrush _sentBubbleBgColor;
        public SolidColorBrush SentBubbleBgColor
        {
            get
            {
                if (_sentBubbleBgColor == null)
                    _sentBubbleBgColor = UI_Utils.Instance.ConvertStringToColor(SentBubbleBackground);

                return _sentBubbleBgColor;
            }
        }

        SolidColorBrush _receivedBubbleBgColor;
        public SolidColorBrush ReceivedBubbleBgColor
        {
            get
            {
                if (_receivedBubbleBgColor == null)
                    _receivedBubbleBgColor = UI_Utils.Instance.ConvertStringToColor(ReceivedBubbleBackground);

                return _receivedBubbleBgColor;
            }
        }

        SolidColorBrush _chatBubbleFgColor;
        public SolidColorBrush BubbleForegroundColor
        {
            get
            {
                if (_chatBubbleFgColor == null)
                    _chatBubbleFgColor = UI_Utils.Instance.ConvertStringToColor(BubbleForeground);

                return _chatBubbleFgColor;
            }
        }

        SolidColorBrush _foregroundColor;
        public SolidColorBrush ForegroundColor
        {
            get
            {
                if (_foregroundColor == null)
                    _foregroundColor = UI_Utils.Instance.ConvertStringToColor(Foreground);

                return _foregroundColor;
            }
        }

        BitmapImage _thumbnailPattern;
        public BitmapImage ThumbnailPattern
        {
            get
            {
                if (_thumbnailPattern == null)
                    _thumbnailPattern = new BitmapImage(new Uri(ThumbnailPath, UriKind.Relative));

                return _thumbnailPattern;
            }
        }

        public Visibility BackgroundVisibility
        {
            get
            {
                return IsDefault ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public Visibility DefaultImageVisibility
        {
            get
            {
                return IsDefault ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        Boolean _isSelected;
        public Boolean IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    NotifyPropertyChanged("IsSelected");
                    NotifyPropertyChanged("BorderColor");
                }
            }
        }

        Visibility _tickImageVisibility = Visibility.Collapsed;
        public Visibility TickImageVisibility
        {
            get
            {
                return _tickImageVisibility;
            }
            set
            {
                if (_tickImageVisibility != value)
                {
                    _tickImageVisibility = value;
                    NotifyPropertyChanged("TickImageVisibility");
                }
            }
        }

        public string ID;
        public string ImagePath;
        public bool IsDefault;
        public bool IsTile;
        public string Background;
        public string SentBubbleBackground;
        public string ReceivedBubbleBackground;
        public string BubbleForeground;
        public string Foreground;
        public Int32 Position;
        public string ThumbnailPath;

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify that a property changed
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("ChatBackground Model :: NotifyPropertyChanged : NotifyPropertyChanged , Exception : " + ex.StackTrace);
                    }
                });
            }
        }
    }
}
