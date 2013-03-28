using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using windows_client.Languages;

namespace windows_client.utils
{
    class MoodsInitialiser
    {
        private static MoodsInitialiser instance = null;

        public static MoodsInitialiser Instance
        {
            get
            {
                if (instance == null) //locks are not required as it would always be used on UI thread
                {
                    instance = new MoodsInitialiser();
                }
                return instance;
            }
        }

        private MoodsInitialiser()
        {
        }

        private BitmapImage[] moodImages = new BitmapImage[33];

        private string[,] moodInfo = new string[,]
        {
            {"/View/images/moods/00Boozing.png",       AppResources.Mood_Boozing           ,                   "",	"",	""},
            {"/View/images/moods/01Insomniac.png",     AppResources.Mood_Insomniac         ,                   "",	"",	""},
            {"/View/images/moods/02Coding.png",        AppResources.Mood_Coding            ,                   "",	"",	""},
            {"/View/images/moods/04Late.png",          AppResources.Mood_Running_late      ,                   "",	"",	""},
            {"/View/images/moods/03Traffic.png",       AppResources.Mood_Stuck_in_traffic  ,                   "",	"",	""},
            {"/View/images/moods/05Shopping.png",      AppResources.Mood_Shopping          ,                   "",	"",	""},
            {"/View/images/moods/08Caffeinating.png",  AppResources.Mood_Caffeinated       ,                   "",	"",	""},
            {"/View/images/moods/09Movie.png",         AppResources.Mood_In_a_movie        ,                   "",	"",	""},
            {"/View/images/moods/07Driving.png",       AppResources.Mood_Driving           ,                   "",	"",	""},
            {"/View/images/moods/06Gaming.png",        AppResources.Mood_Gaming            ,                   "",	"",	""},
            {"/View/images/moods/10Television.png",    AppResources.Mood_Watching_tv       ,                   "",	"",	""},
            {"/View/images/moods/16Busy.png",          AppResources.Mood_Busy              ,                   "",	"",	""},   
            {"/View/images/moods/11MiddleFinger.png",  AppResources.Mood_Middle_finger     ,                   "",	"",	""},   
            {"/View/images/moods/12Love.png",          AppResources.Mood_Love_it           ,                   "",	"",	""},   
            {"/View/images/moods/13Reading.png",       AppResources.Mood_Studying          ,                   "",	"",	""},
            {"/View/images/moods/14Chilling.png",      AppResources.Mood_Chilling          ,                   "",	"",	""},
            {"/View/images/moods/15InLove.png",        AppResources.Mood_In_love           ,                   "",	"",	""},
            {"/View/images/moods/23Hungover.png",      AppResources.Mood_Hungover          ,                   "",	"",	""},
            {"/View/images/moods/17Happy.png",         AppResources.Mood_Happy             ,                   "",	"",	""},
            {"/View/images/moods/18Sad.png",           AppResources.Mood_Sad               ,                   "",	"",	""},
            {"/View/images/moods/19Angry.png",         AppResources.Mood_Angry             ,                   "",	"",	""},
            {"/View/images/moods/22Sleepy.png",        AppResources.Mood_Sleepy            ,                   "",	"",	""},
            {"/View/images/moods/20Confused.png",      AppResources.Mood_Confused          ,                   "",	"",	""},
            { "/View/images/moods/21OMG.png",          AppResources.Mood_Surprised         ,                   "",	"",	""},
            { "/View/images/moods/delhi.png",          AppResources.Mood_Delhi             ,                   "",	"",	""},
            { "/View/images/moods/chennai.png",        AppResources.Mood_Chennai           ,                   "",	"",	""},
            { "/View/images/moods/punjab.png",         AppResources.Mood_Punjab            ,                   "",	"",	""},
            { "/View/images/moods/kolkata.png",        AppResources.Mood_Kolkata           ,                   "",	"",	""},
            { "/View/images/moods/rajasthan.png",      AppResources.Mood_Rajasthan         ,                   "",	"",	""},
            { "/View/images/moods/mumbai.png",         AppResources.Mood_Mumbai            ,                   "",	"",	""},
            { "/View/images/moods/pune.png",           AppResources.Mood_Pune              ,                   "",	"",	""},
            { "/View/images/moods/hyderabad.png",      AppResources.Mood_Hyderabad         ,                   "",	"",	""},
            { "/View/images/moods/bangalore.png",      AppResources.Mood_Bangalore         ,                   "",	"",	""}
        };    


        public BitmapImage GetMoodImageForMoodId(int moodId)
        {
            if (moodId < 1 || moodId > moodImages.Length)
                return null;
            if (moodImages[moodId - 1] == null)
            {
                BitmapImage moodImg = new BitmapImage();
                moodImg.CreateOptions = BitmapCreateOptions.BackgroundCreation;
                moodImg.UriSource = new Uri(moodInfo[moodId - 1, 0], UriKind.Relative);
                moodImages[moodId - 1] = moodImg;
            }
            return moodImages[moodId - 1];
        }

        private List<Mood> _moodList;

        public List<Mood> MoodList
        {
            get
            {
                if (_moodList == null)
                {
                    _moodList = new List<Mood>();
                    for (int i = 0; i < moodImages.Length; i++)
                    {
                        _moodList.Add(new Mood(i + 1));
                    }
                }
                return this._moodList;
            }
        }

        public class Mood
        {
            int _moodId;
            public Mood(int moodId)
            {
                this._moodId = moodId;
            }

            public BitmapImage MoodImage
            {
                get
                {
                    return MoodsInitialiser.Instance.GetMoodImageForMoodId(_moodId);
                }
            }

            public string MoodText
            {
                get
                {
                    if (string.IsNullOrEmpty(MoodsInitialiser.Instance.moodInfo[_moodId - 1, (int)TimeUtils.GetTimeIntervalDay() + 2]))
                    {
                        return MoodsInitialiser.Instance.moodInfo[_moodId - 1, 1]; //if there is no text then return name
                    }
                    return MoodsInitialiser.Instance.moodInfo[_moodId - 1, (int)TimeUtils.GetTimeIntervalDay() + 2];
                }
            }

            public string MoodName
            {
                get
                {
                    return MoodsInitialiser.Instance.moodInfo[_moodId - 1, 1];
                }
            }

        }

    }
}

































































