using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO.IsolatedStorage;
using windows_client;

namespace windows_client.ServerTips
{
    class TipManager
    {
        private static volatile TipManager instance = null;
        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton

        private static readonly string TIPHEADER = "h";
        private static readonly string TIPBODY = "b";
        private static readonly string TIPID = "i";

        //queue for tips
        public TipInfo ChatScreenTip
        {
            get
            {

                if (null == ChatScreenTip || TipState.COMPLETED == ChatScreenTip.State)
                {
                    ChatScreenTip = NextChatScreenTip;
                    NextChatScreenTip = null;
                }

                return ChatScreenTip;
            }
            set
            {
                ChatScreenTip = value;
                ChatScreenTip.State = TipState.SHOWN;

                if (null == ChatScreenTip)
                    App.RemoveKeyFromAppSettings(HikeConstants.CHAT_SCREEN_TIP);
                else
                    App.WriteToIsoStorageSettings(HikeConstants.CHAT_SCREEN_TIP,
                        new TipInfoBase(ChatScreenTip.Type, ChatScreenTip.HeadText, ChatScreenTip.BodyText, ChatScreenTip.TipId));

            }
        }
        public TipInfo MainPageTip
        {
            get
            {

                if (null == MainPageTip || (TipState.COMPLETED == MainPageTip.State))
                {
                    MainPageTip = NextMainPageTip;
                    NextMainPageTip = null;
                }
                return MainPageTip;
            }
            set
            {
                MainPageTip = value;
                MainPageTip.State = TipState.SHOWN;

                if (null == MainPageTip)
                    App.RemoveKeyFromAppSettings(HikeConstants.MAIN_PAGE_TIP);
                else
                    App.WriteToIsoStorageSettings(HikeConstants.MAIN_PAGE_TIP,
                        new TipInfoBase(MainPageTip.Type, MainPageTip.HeadText, MainPageTip.BodyText, MainPageTip.TipId));

            }
        }
        public TipInfo NextChatScreenTip
        {
            get;
            set
            {
                NextChatScreenTip = value;

                if (null == NextChatScreenTip)
                    App.RemoveKeyFromAppSettings(HikeConstants.NEXT_CHAT_SCREEN_TIP);
                else
                    App.WriteToIsoStorageSettings(HikeConstants.NEXT_CHAT_SCREEN_TIP,
                        new TipInfoBase(NextChatScreenTip.Type, NextChatScreenTip.HeadText, NextChatScreenTip.BodyText, NextChatScreenTip.TipId));
            }
        }
        public TipInfo NextMainPageTip
        {
            get;
            set
            {
                NextMainPageTip = value;

                if (null == NextMainPageTip)
                    App.RemoveKeyFromAppSettings(HikeConstants.NEXT_MAIN_PAGE_TIP);
                else
                    App.WriteToIsoStorageSettings(HikeConstants.NEXT_MAIN_PAGE_TIP,
                        new TipInfoBase(NextMainPageTip.Type, NextMainPageTip.HeadText, NextMainPageTip.BodyText, NextMainPageTip.TipId));
            }
        }

        public static TipManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new TipManager();
                    }
                }
                return instance;
            }
        }

        public void InitializeTip(string type, JObject data)
        {
            TipInfo tempTip = new TipInfo(type, (string)data[TIPHEADER], (string)data[TIPBODY], (string)data[TIPID]);
            if (!IsDuplicate(tempTip.TipId))
            {
                if (tempTip.Location == TipInfo.CHATPAGE)
                {
                    NextChatScreenTip = tempTip;
                }
                else if (tempTip.Location == TipInfo.MAINPAGE)
                {
                    NextMainPageTip = tempTip;
                }
            }
        }

        private bool IsDuplicate(string id)
        {

            if (ChatScreenTip != null && ChatScreenTip.TipId.Equals(id))
                return true;

            if (NextChatScreenTip != null && NextChatScreenTip.TipId.Equals(id))
                return true;

            if (MainPageTip != null && MainPageTip.TipId.Equals(id))
                return true;

            if (NextMainPageTip != null && NextMainPageTip.TipId.Equals(id))
                return true;
            return false;
        }

    }
}
