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

namespace windows_client.Model
{
    public class GroupParticipant
    {
        private string _name;
        private string _msisdn;
        private bool _isOnHike;
        private bool _isDND;

        public GroupParticipant()
        { }

        public GroupParticipant(string name, string msisdn, bool isOnHike)
        {
            _name = name;
            _msisdn = msisdn;
            _isOnHike = isOnHike;
            _isDND = false;
        }

        public GroupParticipant(string name, string msisdn, bool isOnHike,bool isDND)
        {
            _name = name;
            _msisdn = msisdn;
            _isOnHike = isOnHike;
            _isDND = isDND;
        }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (value != _name)
                    _name = value;
            }
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

        public bool IsOnHike
        {
            get
            {
                return _isOnHike;
            }
            set
            {
                if (value != _isOnHike)
                    _isOnHike = value;
            }
        }

        public bool IsDND
        {
            get
            {
                return _isDND;
            }
            set
            {
                if (value != _isDND)
                    _isDND = value;
            }
        }
    }
}
