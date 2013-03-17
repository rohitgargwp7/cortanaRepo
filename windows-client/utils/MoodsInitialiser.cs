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
                            {"/View/images/moods/icon40-40/apple.png",""},
                            {"/View/images/moods/icon40-40/beer.png",""},
                            {"/View/images/moods/icon40-40/busy.png",""},
                            {"/View/images/moods/icon40-40/camera.png",""},
                            {"/View/images/moods/icon40-40/car.png",""},
                            {"/View/images/moods/icon40-40/confused.png",""},
                            {"/View/images/moods/icon40-40/dumble.png",""},
                            {"/View/images/moods/icon40-40/game.png",""},
                            {"/View/images/moods/icon40-40/happy.png",""},
                            {"/View/images/moods/icon40-40/heart.png",""},
                            {"/View/images/moods/icon40-40/hungover.png",""},
                            {"/View/images/moods/icon40-40/laugh.png",""},
                            {"/View/images/moods/icon40-40/music.png",""},
                            {"/View/images/moods/icon40-40/pop_corn.png",""},
                            {"/View/images/moods/icon40-40/rain.png",""},
                            {"/View/images/moods/icon40-40/reader.png",""},
                            {"/View/images/moods/icon40-40/sad.png",""},
                            {"/View/images/moods/icon40-40/scooter.png",""},
                            {"/View/images/moods/icon40-40/sleepy.png",""},
                            {"/View/images/moods/icon40-40/sun.png",""},
                            {"/View/images/moods/icon40-40/surprise.png",""},
                            {"/View/images/moods/icon40-40/tea.png",""},
                            {"/View/images/moods/icon40-40/tv.png",""},
                            {"/View/images/moods/icon40-40/write.png",""}
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
                listMoods.Add(new Mood(img, moodsPaths[i, 1]));
            }
            isInitialised = true;
        }

        public BitmapImage GetMoodIcon(int moodId)
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
        string text;

        public Mood(BitmapImage moodIcon, string text)
        {
            this.moodIcon = moodIcon;
            this.text = text;
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
        public string Text
        {
            get
            {
                return text;
            }
            set
            {
                text = value;
            }
        }
    }
}
