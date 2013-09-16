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

namespace windows_client.Model
{
    public class ChatBackground
    {
        SolidColorBrush _backgroundColor;
        public SolidColorBrush BackgroundColor
        {
            get
            {
                if(_backgroundColor == null)
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

        public BitmapImage ImagePattern
        {
            get
            {
                return new BitmapImage(new Uri(Pattern,UriKind.Relative));
            }
        }

        public string ID;
        public string Pattern;
        public bool IsTile;
        public string Background;
        public string SentBubbleBackground;
        public string ReceivedBubbleBackground;
        public string BubbleForeground;
        public string Foreground;
        public Int32 Position;
        public string Thumbnail;

        public void Write(BinaryWriter writer)
        {
            try
            {
                writer.WriteStringBytes(ID);

                if (Pattern == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(Pattern);

                writer.Write(IsTile);

                if (Background == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(Background);

                if (SentBubbleBackground == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(SentBubbleBackground);
               
                if (ReceivedBubbleBackground == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(ReceivedBubbleBackground);
               
                if (BubbleForeground == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(BubbleForeground);

                if (Foreground == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(Foreground);

                writer.Write(Position);

                if (Thumbnail == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(Thumbnail);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ChatBackground Model :: Write : Unable To write, Exception : " + ex.StackTrace);
            }

        }

        public void Read(BinaryReader reader)
        {
            try
            {
                int count = reader.ReadInt32();
                ID = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);

                count = reader.ReadInt32();
                Pattern = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                if (Pattern == "*@N@*")
                    Pattern = null;

                IsTile = reader.ReadBoolean();

                count = reader.ReadInt32();
                Background = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                if (Background == "*@N@*")
                    Background = null;

                count = reader.ReadInt32();
                SentBubbleBackground = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                if (SentBubbleBackground == "*@N@*")
                    SentBubbleBackground = null;

                count = reader.ReadInt32();
                ReceivedBubbleBackground = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                if (ReceivedBubbleBackground == "*@N@*")
                    ReceivedBubbleBackground = null;

                count = reader.ReadInt32();
                BubbleForeground = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                if (BubbleForeground == "*@N@*")
                    BubbleForeground = null;

                count = reader.ReadInt32();
                Foreground = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                if (Foreground == "*@N@*")
                    Foreground = null;

                Position = reader.ReadInt32();

                count = reader.ReadInt32();
                Thumbnail = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                if (Thumbnail == "*@N@*")
                    Thumbnail = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ChatBackground Model :: Read : Read, Exception : " + ex.StackTrace);
            }
        }

        
    }
}
