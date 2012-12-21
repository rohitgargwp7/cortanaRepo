using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using windows_client.Misc;
using windows_client.utils;

namespace windows_client.Model
{
    public class Favourites : IBinarySerializable
    {
        private string _msisdn;
        private string _contactname;
        private bool _onHike;
        private byte[] _avatar;

        public Favourites(string mContactNumber, string mContactName, bool isOnHike,BitmapImage avatarImg)
        {
            _msisdn = mContactNumber;
            _contactname = mContactName;
            _onHike = isOnHike;
            AvatarImage = avatarImg;
        }

        public Favourites(string mContactNumber, string mContactName, bool isOnHike, byte [] avatar)
        {
            _msisdn = mContactNumber;
            _contactname = mContactName;
            _onHike = isOnHike;
            _avatar = avatar;
        }

        public Favourites()
        {
            
        }

        public string Msisdn
        {
            get
            {
                return _msisdn;
            }
            set
            {
                if (value != _msisdn)
                    _msisdn = value;
            }
        }
        public string ContactName
        {
            get
            {
                return _contactname;
            }
            set
            {
                if (value != _contactname)
                    _contactname = value;
            }
        }
        public byte[] Image
        {
            get
            {
                return _avatar;
            }
            set
            {
                if (value != _avatar)
                    _avatar = value;
            }
        }
        public bool OnHike
        {
            get
            {
                return _onHike;
            }
            set
            {
                if (value != _onHike)
                    _onHike = value;
            }
        }

        public BitmapImage AvatarImage
        {
            get
            {
                try
                {
                    if (Image == null)
                    {
                        if (Utils.isGroupConversation(Msisdn))
                            return UI_Utils.Instance.DefaultGroupImage;
                        return UI_Utils.Instance.DefaultAvatarBitmapImage;
                    }
                    else
                    {
                        MemoryStream memStream = new MemoryStream(Image);
                        memStream.Seek(0, SeekOrigin.Begin);
                        BitmapImage empImage = new BitmapImage();
                        empImage.SetSource(memStream);
                        return empImage;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Exception in Avatar Image : {0}", e.ToString());
                    return null;
                }
            }
            set
            {
                if (value != AvatarImage)
                    AvatarImage = value;
            }
        }

        public void Write(BinaryWriter writer)
        {
            try
            {
                if (Msisdn == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(Msisdn);

                if (ContactName == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(ContactName);
                writer.Write(OnHike);
            }
            catch
            {
                throw new Exception("Unable to write to favourite file...");
            }
        }

        public void Read(BinaryReader reader)
        {
            try
            {
                int count = reader.ReadInt32();
                Msisdn = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                if (Msisdn == "*@N@*")
                    Msisdn = null;
                count = reader.ReadInt32();
                ContactName = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                if (ContactName == "*@N@*")
                    ContactName = null;
                OnHike = reader.ReadBoolean();
            }
            catch { }
        }
    }
}
