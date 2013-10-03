using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.IO.IsolatedStorage;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using windows_client.Misc;
using windows_client.utils;
using System.Windows;

namespace windows_client.Model
{
    public class ProTip
    {
        public string Id;
        public string Header;
        public string Body;
        public string ImageUrl;
        public string Base64Image;

        ImageSource _tipImage;
        public ImageSource TipImage
        {
            get
            {
                if (_tipImage == null)
                {
                    if (Base64Image != null)
                        _tipImage = UI_Utils.Instance.createImageFromBytes(System.Convert.FromBase64String(Base64Image));
                    else
                        _tipImage = ProcesImageSource();

                    return _tipImage;
                }
                else
                    return _tipImage;
            }
        }

        private ImageSource ProcesImageSource()
        {
            ImageSource source = null;

            source = new BitmapImage();

            if (!String.IsNullOrEmpty(ImageUrl))
                ImageLoader.Load(source as BitmapImage, new Uri(ImageUrl), null, Utils.ConvertUrlToFileName(ImageUrl));

            return source;
        }

        public ProTip() { }

        public ProTip(string id, string header, string body, string imageUrl, string base64Image)
        {
            Id = id;
            Header = header;
            Body = body;
            ImageUrl = imageUrl;
            Base64Image = base64Image;
        }

        public void Write(BinaryWriter writer)
        {
            try
            {
                writer.WriteStringBytes(Id);

                if (Header == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(Header);

                if (Body == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(Body);

                if (ImageUrl == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(ImageUrl);

                if (Base64Image == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(Base64Image);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ProTip :: Write : Unable To write, Exception : " + ex.StackTrace);
            }

        }

        public void Read(BinaryReader reader)
        {
            try
            {
                int count = reader.ReadInt32();
                Id = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);

                count = reader.ReadInt32();
                Header = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                if (Header == "*@N@*")
                    Header = null;

                count = reader.ReadInt32();
                Body = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                if (Body == "*@N@*")
                    Body = null;

                count = reader.ReadInt32();
                ImageUrl = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                if (ImageUrl == "*@N@*")
                    ImageUrl = null;

                try
                {
                    count = reader.ReadInt32();
                    Base64Image = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                    if (Base64Image == "*@N@*")
                        Base64Image = null;
                }
                catch
                {
                    Base64Image = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ProTip :: Read : Read, Exception : " + ex.StackTrace);
            }
        }
    }
}
