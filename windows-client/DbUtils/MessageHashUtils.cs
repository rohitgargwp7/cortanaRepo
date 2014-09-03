using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using windows_client.Model;

namespace windows_client.DbUtils
{
    public static class MessageHashUtils
    {

        public static bool addHashMessage(MessageHash messageHash)
        {
            if (messageHash.Messagehash > 0)
            {
                using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring + ";Max Buffer Size = 1024;"))
                {

                    //IQueryable<ConvMessage> qq = DbCompiledQueries.GetMessageForMappedMsgIdMsisdn(context, messageHash.Msisdn, messageHash.MappedMessageId, messageHash.Message);
                    //ConvMessage cm = qq.FirstOrDefault();
                    //if (cm != null)
                    //    return false;

                    context.messageHash.InsertOnSubmit(messageHash);

                    try
                    {
                        context.SubmitChanges();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("MessagesTableUtils :: addMessage : submit changes, Exception : " + ex.StackTrace);
                        return false;
                    }
                }
            }
            return false;
        }

        public static List<MessageHash> addHashMessage(IEnumerable<MessageHash> messageHashList)
        {
            if (messageHashList == null || messageHashList.Count() == 0)
                return null;
            List<MessageHash> listSuccesMessages = new List<MessageHash>();
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring + ";Max Buffer Size = 1024;"))
            {

                {

                    foreach (MessageHash messagehash in messageHashList)
                    //IQueryable<ConvMessage> qq = DbCompiledQueries.GetMessageForMappedMsgIdMsisdn(context, messageHash.Msisdn, messageHash.MappedMessageId, messageHash.Message);
                    //ConvMessage cm = qq.FirstOrDefault();
                    //if (cm != null)
                    //    return false;
                    {
                        context.messageHash.InsertOnSubmit(messagehash);
                        try
                        {
                            context.SubmitChanges(System.Data.Linq.ConflictMode.ContinueOnConflict);
                            listSuccesMessages.Add(messagehash);
                        }
                        catch (Exception ex)
                        {
                            //Debug.WriteLine("Duplocate messageHash, Exception : " + ex.StackTrace);
                        }
                    }
                }

            }
            return listSuccesMessages;
        }
    }
}
