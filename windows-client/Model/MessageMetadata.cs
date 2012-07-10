using Newtonsoft.Json.Linq;

namespace windows_client.Model
{
    public class MessageMetadata
    {
        private string dndMissedCallNumber;
        private JObject json;

        public MessageMetadata(JObject metadata)
        {
            this.dndMissedCallNumber = (string)metadata[HikeConstants.METADATA_DND];
            this.json = metadata;
        }

        public string getDNDMissedCallNumber()
        {
            return dndMissedCallNumber;
        }

        public string serialize()
        {
            return this.json.ToString(); ;
        }
    }
}
