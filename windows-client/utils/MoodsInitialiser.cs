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

        private BitmapImage[] moodImages = new BitmapImage[23];

        private string[,] moodInfo = new string[,]
        {
            {"/View/images/moods/apple.png",   AppResources.Mood_Food_Txt,       AppResources.Mood_Food_Morning_Txt,	AppResources.Mood_Food_Noon_Txt,	AppResources.Mood_Food_Night_Txt},
            {"/View/images/moods/sun.png",     AppResources.Mood_Sunny_Txt,      AppResources.Mood_Sunny_Morning_Txt,	AppResources.Mood_Sunny_Noon_Txt,	AppResources.Mood_Sunny_Night_Txt},
            {"/View/images/moods/rain.png",    AppResources.Mood_Rainy_Txt,      AppResources.Mood_Rainy_Morning_Txt,	AppResources.Mood_Rainy_Noon_Txt,	AppResources.Mood_Rainy_Night_Txt},
            {"/View/images/moods/sleepy.png",  AppResources.Mood_Sleep_Txt,      AppResources.Mood_Sleep_Morning_Txt,	AppResources.Mood_Sleep_Noon_Txt,	AppResources.Mood_Sleep_Night_Txt},
            {"/View/images/moods/tea.png",     AppResources.Mood_Coffee_Txt,     AppResources.Mood_Coffee_Morning_Txt,	AppResources.Mood_Coffee_Noon_Txt,	AppResources.Mood_Coffee_Night_Txt},
            {"/View/images/moods/pop_corn.png",AppResources.Mood_Popcorn_Txt,    AppResources.Mood_Popcorn_Morning_Txt,	AppResources.Mood_Popcorn_Noon_Txt,	AppResources.Mood_Popcorn_Night_Txt},
            {"/View/images/moods/dumble.png",  AppResources.Mood_Gym_Txt,        AppResources.Mood_Gym_Morning_Txt,	AppResources.Mood_Gym_Noon_Txt,	AppResources.Mood_Gym_Night_Txt},
            {"/View/images/moods/car.png",     AppResources.Mood_Car_Txt,        AppResources.Mood_Car_Morning_Txt,	AppResources.Mood_Car_Noon_Txt,	AppResources.Mood_Car_Night_Txt},
            {"/View/images/moods/scooter.png", AppResources.Mood_Bike_Txt,       AppResources.Mood_Bike_Morning_Txt,	AppResources.Mood_Bike_Noon_Txt,	AppResources.Mood_Bike_Night_Txt},
            {"/View/images/moods/tv.png",      AppResources.Mood_TV_Txt,         AppResources.Mood_TV_Morning_Txt,	AppResources.Mood_TV_Noon_Txt,	AppResources.Mood_TV_Night_Txt},
            {"/View/images/moods/beer.png",    AppResources.Mood_Beer_Txt,       AppResources.Mood_Beer_Morning_Txt,	AppResources.Mood_Beer_Noon_Txt,	AppResources.Mood_Beer_Night_Txt},
            {"/View/images/moods/hungover.png",AppResources.Mood_Sick_Txt,       "",	"",	""},
            {"/View/images/moods/game.png",    AppResources.Mood_Game_Txt,       "",	"",	""},
            {"/View/images/moods/music.png",   AppResources.Mood_Music_Txt,      "",	"",	""},
            {"/View/images/moods/reader.png",  AppResources.Mood_Reading_Txt,    "",	"",	""},
            {"/View/images/moods/heart.png",   AppResources.Mood_Love_Txt,       "",	"",	""},
            {"/View/images/moods/write.png",   AppResources.Mood_Writing_Txt,    "",	"",	""},
            {"/View/images/moods/happy.png",   AppResources.Mood_Happy_Txt,      "",	"",	""},
            {"/View/images/moods/sad.png",     AppResources.Mood_Sad_Txt,        "",	"",	""},
            {"/View/images/moods/confused.png",AppResources.Mood_Exhausted_Txt,  "",	"",	""},
            {"/View/images/moods/surprise.png",AppResources.Mood_OMG_Txt,        "",	"",	""},
            {"/View/images/moods/laugh.png",   AppResources.Mood_LOL_Txt,        "",	"",	""},
            {"/View/images/moods/busy.png",    AppResources.Mood_Busy_Txt,       "",	"",	""}
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
