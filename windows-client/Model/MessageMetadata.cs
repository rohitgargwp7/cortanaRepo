using Newtonsoft.Json.Linq;

namespace windows_client.Model
{
    /*TODO :  Put proper json inside this*/
    public class MessageMetadata
    {
        private string dndMissedCallNumber;
        private bool newUser;
        private JObject json;
        private JArray dndNumbers;
        private ConvMessage.ParticipantInfoState participantInfoState = ConvMessage.ParticipantInfoState.NO_INFO;

        public MessageMetadata(JObject metadata)
        {
            this.newUser = (string)metadata[HikeConstants.NEW_USER] == "true";
            this.dndNumbers = (JArray)metadata[HikeConstants.DND_NUMBERS];
            this.participantInfoState = ConvMessage.fromJSON(metadata);
            this.dndMissedCallNumber = (string)metadata[HikeConstants.METADATA_DND];
            this.json = metadata;
        }

        public string getDNDMissedCallNumber()
        {
            return dndMissedCallNumber;
        }

        public string Serialize
        {
            get
            {
                if(json != null)
                    return this.json.ToString(Newtonsoft.Json.Formatting.None);
                return null;
            }
        }

        public JObject JsonObj
        {
            set
            {
                if (value != json)
                    json = value;
            }
        }

        public ConvMessage.ParticipantInfoState ParticipantState
        {
            get
            {
                return participantInfoState;
            }
            set
            {
                if (value != participantInfoState)
                    participantInfoState = value;
            }
        }
    }
}
