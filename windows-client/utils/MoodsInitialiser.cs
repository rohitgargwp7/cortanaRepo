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
        private string[,] moodsPaths = new string[,]
        {
        	{
                "/View/images/emoticons/emo_im_01_bigsmile.png",
                "smile"
            },
            {
                "/View/images/emoticons/emo_im_02_happy.png",
                ""
            },
            {
			"/View/images/emoticons/emo_im_03_laugh.png",""},
            {
        	"/View/images/emoticons/emo_im_04_smile.png",""},
            {
			"/View/images/emoticons/emo_im_05_wink.png",""},
            {
			"/View/images/emoticons/emo_im_06_adore.png",""},
            {
			"/View/images/emoticons/emo_im_07_kiss.png",""},
            {
			"/View/images/emoticons/emo_im_08_kissed.png",""},
            {
            "/View/images/emoticons/emo_im_09_expressionless.png",""},
            {
			"/View/images/emoticons/emo_im_10_pudently.png",""}
        };

        public void Initialise()
        {
            if (isInitialised)
                return;
            listMoods = new List<Mood>();
            for (int i = 0; i < 9; i++)
            {
                BitmapImage img = new BitmapImage();
                img.CreateOptions = BitmapCreateOptions.BackgroundCreation;
                img.UriSource = new Uri(moodsPaths[i, 0], UriKind.Relative);
                listMoods.Add(new Mood(img, moodsPaths[i, 1]));
            }
            isInitialised = true;
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
