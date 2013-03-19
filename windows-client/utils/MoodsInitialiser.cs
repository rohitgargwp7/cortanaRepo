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
                listMoods.Add(new Mood(img, moodsPaths[i, 1], moodsPaths[i, 2], moodsPaths[i, 3], moodsPaths[i, 4]));
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
        string name;
        string morningText;
        string dayText;
        string nightText;

        public Mood(BitmapImage moodIcon, string name, string morningText, string dayText, string nightText)
        {
            this.moodIcon = moodIcon;
            this.morningText = morningText;
            this.dayText = dayText;
            this.nightText = nightText;
            this.name = name;
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
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
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

