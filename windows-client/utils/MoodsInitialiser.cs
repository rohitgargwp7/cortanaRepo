using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

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

        private BitmapImage[] moodImages = new BitmapImage[24];

        private string[,] moodInfo = new string[,]
        {
            {"/View/images/moods/apple.png","Food", "A healthy food, for a wealthy mood!","Eating a delicious meal.","Eating a delicious meal."},
            {"/View/images/moods/beer.png","Beer","Have no fear, I've got beer...","Have no fear, I've got beer...","Where there’s life, there’s Beer..."},
            {"/View/images/moods/busy.png","Busy","","",""},
            {"/View/images/moods/camera.png","","","",""},
            {"/View/images/moods/car.png","Car","On the road...","On the road...","On the road..."},
            {"/View/images/moods/confused.png","Exhausted","","",""},
            {"/View/images/moods/dumble.png","Gym","No pain, no gain.","No pain, no gain.","Too fit to quit!"},
            {"/View/images/moods/game.png","Game","","",""},
            {"/View/images/moods/happy.png","Happy","","",""},
            {"/View/images/moods/heart.png","Love","","",""},
            {"/View/images/moods/hungover.png","Sick","","",""},
            {"/View/images/moods/laugh.png","LOL","","",""},
            {"/View/images/moods/music.png","Music","","",""},
            {"/View/images/moods/pop_corn.png","Popcorn","Movie time!","Movie time!","Movie time!"},
            {"/View/images/moods/rain.png","Rainy","Its raining, its pouring...","Its raining, its pouring...","Its raining, its pouring..."},
            {"/View/images/moods/reader.png","Reading","","",""},
            {"/View/images/moods/sad.png","Sad","","",""},
            {"/View/images/moods/scooter.png","Bike","If wheels could fly, I'd be scooterman...","If wheels could fly, I'd be scooterman...","If wheels could fly, I'd be scooterman..."},
            {"/View/images/moods/sleepy.png","Sleep","Sleeeepppyyy...","Sleeeepppyyy...","Yawnnn..."},
            {"/View/images/moods/sun.png","Sunny","What a beautiful morning","What a beautiful day","What a beautiful day"},
            {"/View/images/moods/surprise.png","OMG","","",""},
            {"/View/images/moods/tea.png","Coffee","My favorite morning pick me up.","Caffeinated...","Caffeinated..."},
            {"/View/images/moods/tv.png","TV","Watching TV...","Watching TV...","Watching Prime Time..."},
            {"/View/images/moods/write.png","Writing","","",""}
        };


        public BitmapImage GetMoodImageForMoodId(int moodId)
        {
            if (moodId < 1 || moodId > moodImages.Length)
                return null;
            if (moodImages[moodId] == null)
            {
                BitmapImage moodImg = new BitmapImage();
                moodImg.CreateOptions = BitmapCreateOptions.BackgroundCreation;
                moodImg.UriSource = new Uri(moodInfo[moodId, 0], UriKind.Relative);
                moodImages[moodId] = moodImg;
            }
            return moodImages[moodId];
        }

        private List<Moods> _moodList;

        public List<Moods> MoodList
        {
            get
            {
                if (_moodList == null)
                {
                    _moodList = new List<Moods>();
                    for (int i = 0; i < moodInfo.Length; i++)
                    {
                        _moodList.Add(new Moods(i + 1));
                    }
                }
                return this._moodList;
            }
        }

        public class Moods
        {
            int _moodId;
            public Moods(int moodId)
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
                    if (string.IsNullOrEmpty(MoodsInitialiser.Instance.moodInfo[_moodId, (int)TimeUtils.GetTimeIntervalDay()]))
                    {
                        return MoodsInitialiser.Instance.moodInfo[_moodId, (int)TimeUtils.GetTimeIntervalDay()];
                    }
                    return MoodsInitialiser.Instance.moodInfo[_moodId, 1]; //if there is no text then return name
                }
            }
        }

    }
}
