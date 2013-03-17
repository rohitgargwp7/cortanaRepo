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
        public List<Mood> listMoods;
        bool isInitialised = false;
        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton
        private static volatile MoodsInitialiser instance = null;

        public static MoodsInitialiser Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new MoodsInitialiser();
                    }
                }
                return instance;
            }
        }

        private MoodsInitialiser()
        {
        }

        private const int moodscount = 24;
        private string[,] moodsPaths = new string[,]
                        {
                            {"/View/images/moods/icon40-40/apple.png", "A healthy food, for a wealthy mood!","Eating a delicious meal.","Eating a delicious meal."},
                            {"/View/images/moods/icon40-40/beer.png", "Have no fear, I've got beer...","Have no fear, I've got beer...","Where there’s life, there’s Beer..."},
                            {"/View/images/moods/icon40-40/busy.png", "","",""},
                            {"/View/images/moods/icon40-40/camera.png","","",""},
                            {"/View/images/moods/icon40-40/car.png",   "On the road...","On the road...","On the road..."},
                            {"/View/images/moods/icon40-40/confused.png","","",""},
                            {"/View/images/moods/icon40-40/dumble.png",  "No pain, no gain.","No pain, no gain.","Too fit to quit!"},
                            {"/View/images/moods/icon40-40/game.png",    "","",""},
                            {"/View/images/moods/icon40-40/happy.png",   "","",""},
                            {"/View/images/moods/icon40-40/heart.png",   "","",""},
                            {"/View/images/moods/icon40-40/hungover.png","","",""},
                            {"/View/images/moods/icon40-40/laugh.png",   "","",""},
                            {"/View/images/moods/icon40-40/music.png",   "","",""},
                            {"/View/images/moods/icon40-40/pop_corn.png","Movie time!","Movie time!","Movie time!"},
                            {"/View/images/moods/icon40-40/rain.png",    "Its raining, its pouring...","Its raining, its pouring...","Its raining, its pouring..."},
                            {"/View/images/moods/icon40-40/reader.png",  "","",""},
                            {"/View/images/moods/icon40-40/sad.png",     "","",""},
                            {"/View/images/moods/icon40-40/scooter.png", "","",""},
                            {"/View/images/moods/icon40-40/sleepy.png",  "Sleeeepppyyy...","Sleeeepppyyy...","Yawnnn..."},
                            {"/View/images/moods/icon40-40/sun.png",     "What a beautiful morning","What a beautiful day","What a beautiful day"},
                            {"/View/images/moods/icon40-40/surprise.png","","",""},
                            {"/View/images/moods/icon40-40/tea.png",     "My favorite morning pick me up.","Caffeinated...","Caffeinated..."},
                            {"/View/images/moods/icon40-40/tv.png",      "","",""},
                            {"/View/images/moods/icon40-40/write.png","","",""}
                        };

        public void Initialise()
        {
            if (isInitialised)
                return;
            listMoods = new List<Mood>();
            for (int i = 0; i < moodscount; i++)
            {
                BitmapImage img = new BitmapImage();
                img.CreateOptions = BitmapCreateOptions.BackgroundCreation;
                img.UriSource = new Uri(moodsPaths[i, 0], UriKind.Relative);
                listMoods.Add(new Mood(img, moodsPaths[i, 1], moodsPaths[i, 2], moodsPaths[i, 3]));
            }
            isInitialised = true;
        }

        public BitmapImage getMoodImage(int moodId)
        {
            //todo:need to initialise on Appload or for Lazy load check always
            if (!isInitialised)
            {
                Initialise();
            }

            if (listMoods != null && moodId < 0 || moodId > listMoods.Count - 1)
                return null;

            return listMoods[moodId].MoodIcon;
        }

    }
    class Mood
    {
        BitmapImage moodIcon;
        string morningText;
        string dayText;
        string nightText;

        public Mood(BitmapImage moodIcon, string morningText, string dayText, string nightText)
        {
            this.moodIcon = moodIcon;
            this.morningText = morningText;
            this.dayText = dayText;
            this.nightText = nightText;
        }

        public BitmapImage MoodIcon
        {
            get
            {
                return moodIcon;
            }
            set
            {
                moodIcon = value;
            }
        }
        public string MorningText
        {
            get
            {
                return morningText;
            }
            set
            {
                morningText = value;
            }
        }

        public string DayText
        {
            get
            {
                return dayText;
            }
            set
            {
                dayText = value;
            }
        }
        public string NightText
        {
            get
            {
                return nightText;
            }
            set
            {
                nightText = value;
            }
        }

        public string DisplayText
        {
            get
            {
                switch (TimeUtils.GetTimeIntervalDay())
                {
                    case 0:
                        return morningText;
                    case 1:
                        return dayText;
                    default:
                        return nightText;

                }
            }
        }

    }
}













