using CommonLibrary.Lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary.Model
{
    public class GroupPic : IBinarySerializable
    {
        public GroupPic()
        {
        }

        public GroupPic(string id)
        {
            GroupId = id;
            IsRetried = false;
        }

        public string GroupId
        {
            get;
            set;
        }

        public bool IsRetried
        {
            get;
            set;
        }

        public void Write(BinaryWriter writer)
        {
            writer.WriteString(GroupId);
            writer.Write(IsRetried);
        }

        public void Read(BinaryReader reader)
        {
            GroupId = reader.ReadString();
            IsRetried = reader.ReadBoolean();
        }
    }
}
