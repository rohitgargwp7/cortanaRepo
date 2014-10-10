using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Runtime.Serialization;
using CommonLibrary.Lib;
using CommonLibrary.Misc;

namespace CommonLibrary.Model
{
    [DataContract]
    public class GroupParticipant : IComparable<GroupParticipant>, IBinarySerializable
    {
        private string _grpId;
        private string _name; // this is full name
        private string _msisdn;
        private bool _hasLeft;
        private bool _isOnHike;
        private bool _isDND;
        private bool _hasOptIn;
        private bool _isUsed;

        public GroupParticipant()
        { }

        public GroupParticipant(string grpId, string name, string msisdn, bool isOnHike)
        {
            _grpId = grpId;
            _name = name;
            _msisdn = msisdn;
            _isOnHike = isOnHike;
            _isDND = true;
            _hasOptIn = false;
            _hasLeft = false;
        }

        public GroupParticipant(string name, string msisdn, bool isOnHike)
        {
            _name = name;
            _msisdn = msisdn;
            _isOnHike = isOnHike;
            _isDND = false;
            _hasOptIn = false;
        }

        public GroupParticipant(string name, string msisdn, bool isOnHike, bool isDND)
        {
            _name = name;
            _msisdn = msisdn;
            _isOnHike = isOnHike;
            _isDND = isDND;
        }

        public string GroupId
        {
            get
            {
                return _grpId;
            }
            set
            {
                if (value != _grpId)
                    _grpId = value;
            }
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

        public string FirstName
        {
            get
            {
                if (string.IsNullOrEmpty(_name))
                    return null;
                _name = _name.Trim();
                int idx = _name.IndexOf(" ");
                if (idx != -1)
                    return _name.Substring(0, idx);
                else
                    return _name;
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

        public bool HasOptIn
        {
            get
            {
                return _hasOptIn;
            }
            set
            {
                if (value != _hasOptIn)
                    _hasOptIn = value;
            }
        }

        public bool HasLeft
        {
            get
            {
                return _hasLeft;
            }
            set
            {
                if (value != _hasLeft)
                    _hasLeft = value;
            }
        }

        public bool IsUsed
        {
            get
            {
                return _isUsed;
            }
            set
            {
                if (value != _isUsed)
                    _isUsed = value;
            }
        }

        bool _isInAddressBook;
        public bool IsInAddressBook
        {
            get
            {
                return _isInAddressBook;
            }
            set
            {
                if (value != _isInAddressBook)
                    _isInAddressBook = value;
            }
        }

        bool _isOwner;
        public bool IsOwner
        {
            get
            {
                return _isOwner;
            }
            set
            {
                if (value != _isOwner)
                    _isOwner = value;
            }
        }

        public bool IsFav
        {
            get
            {
                if (HikeInstantiation.ViewModel.Isfavourite(_msisdn))
                    return true;
                return false;
            }
        }

        public int CompareTo(GroupParticipant rhs)
        {
            return (this.Name.ToLower().CompareTo(((GroupParticipant)rhs).Name.ToLower()));
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            GroupParticipant o = obj as GroupParticipant;

            if ((System.Object)o == null)
            {
                return false;
            }
            return (_msisdn == o.Msisdn);
        }

        public void Write(BinaryWriter writer)
        {
            try
            {
                if (_grpId == null)
                    writer.WriteStringBytes(string.Empty);
                else
                    writer.WriteStringBytes(_grpId);

                if (_name == null)
                    writer.WriteStringBytes(string.Empty);
                else
                    writer.WriteStringBytes(_name);

                if (_msisdn == null)
                    writer.WriteStringBytes(string.Empty);
                else
                    writer.WriteStringBytes(_msisdn);
                writer.Write(_hasLeft);
                writer.Write(_isOnHike);
                writer.Write(_isDND);
                writer.Write(_hasOptIn);
                writer.Write(_isUsed);
                writer.Write(_isInAddressBook);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("GroupParticipant ::  Write : Write, Exception : " + ex.StackTrace);
                throw new Exception("Unable to write to a file...");
            }
        }

        public void Read(BinaryReader reader)
        {
            try
            {
                int count = reader.ReadInt32();
                _grpId = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                if (_grpId == string.Empty)
                    _grpId = null;
                count = reader.ReadInt32();
                _name = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                if (_name == string.Empty) // this is done so that we can specifically set null if contact name is not there
                    _name = null;
                count = reader.ReadInt32();
                _msisdn = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                if (_msisdn == string.Empty)
                    _msisdn = null;
                _hasLeft = reader.ReadBoolean();
                _isOnHike = reader.ReadBoolean();
                _isDND = reader.ReadBoolean();
                _hasOptIn = reader.ReadBoolean();
                _isUsed = reader.ReadBoolean();
                _isInAddressBook = reader.ReadBoolean();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("GroupParticipant ::  Read : Read, Exception : " + ex.StackTrace);
                throw new Exception("Conversation Object corrupt");
            }
        }
    }
}
