﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace windows_client
{
    public class SmileyParser
    {
        //121 is missing
        public static string[] emoticonStrings = 
        {
            ":))",  // 01 bigsmile 
            ":-)",  // 02 happy 
            ":-D",  // 03 laugh 
            "=)",  // 04 smile 
            ";)",  // 05 wink 
            ":-X",  // 06 adore 
            ":-*",  // 07 kiss 
            "(kissed)",  // 08 kissed
            ":-|",  // 09 expressionless 
            ":\")",  // 10 pudently 
            "^.^",  // 11 satisfied 
            "(giggle)",  // 12 giggle 
            ":-P",  // 13 impish 
            "=\\",  // 14 disappointment 
            ";-)",  // 15 beuptonogood 
            "X[",  // 16 frustrated 
            ":-(",  // 17 sad 
            ":?-(",  // 18 sorry 
            ":'-(",  // 19 cry 
            "l-o",  // 20 boring 
            ":0",  // 21 hungry 
            "(scared)",  // 22 scared 
            "o_o",  // 23 shock 
            "(sweat)",  // 24 sweat 
            "T_T",  // 25 crying 
            ":D",  // 26 lol 
            ":o",  // 27 woo 
            ":-O",  // 28 surprise 
            ":-&lt;",  // 29 frown 
            "X(",  // 30 angry 
            "(wornout)",  // 31 wornout 
            "(stop)",  // 32 stop 
            "X-(",  // 33 furious 
            "(smoking)",  // 34 smoking 
            "XD",  // 35 hysterical 
            ":@",  // 36 exclamation 
            ":-Q",  // 37 question 
            "u_u",  // 38 sleep 
            ":-Z",  // 39 aggressive 
            ":-=",  // 40 badly 
            "(^o^)",  // 41 singing 
            "(@=)",  // 42 bomb 
            "b-(",  // 43 beaten 
            ":-q",  // 44 thumbsdown 
            ":-b",  // 45 thumbsup 
            "(beer)",  // 46 beer 
            ":-c",  // 47 call 
            "(hi)",  // 48 hi 
            "(hug)",  // 49 hug 
            "(face palm)",  // 50 facepalm 
            "$-)",  // 51 easymoney 
            "%-}",  // 52 dizzy 
            "DX",  // 53 disgust 
            "(/)",  // 54 cocktail 
            "(coffee)",  // 55 coffee 
            ":-`|",  // 56 cold 
            "B-)",  // 57 cool 
            ":-E",  // 58 despair 
            "(@-))",  // 59 hypnotic 
            "%-)",  // 60 stars 
            "!:-)",  // 61 idea 
            "(monocle)",  // 62 monocle 
            "(movie)",  // 63 movie 
            "(music)",  // 64 music 
            ":-B",  // 65 nerd 
            "(ninja)",  // 66 ninja 
            "&lt;:--)",  // 67 party 
            "P-(",  // 68 pirate 
            ":-@",  // 69 rage 
            "(@>---)",  // 70 rose 
            ":-s",  // 71 sick 
            "(snotty)",  // 72 snotty 
            "-.-",  // 73 stressed 
            "(struggle)",  // 74 struggle 
            "(study)",  // 75 study 
            "O:-)",  // 76 sweetangel 
            "*-)",  // 77 thinking 
            ":-w",  // 78 waiting 
            ":-\"",  // 79 whistling 
            "(yawn)",  // 80 yawn 
            "(exciting1)",  // 81 exciting 
            "(big smile1)",  // 82 big smile 
            "(haha1)",  // 83 haha 
            "(victory1)",  // 84 victory 
            "(red heart1)",  // 85 red heart 
            "(amazing1)",  // 86 amazing 
            "(black heart1)",  // 87 black heart 
            "(what1)",  // 88 what 
            "(bad smile1)",  // 89 bad smile 
            "(bad egg1)",  // 90 bad egg 
            "(grimace1)",  // 91 grimace 
            "(girl1)",  // 92 girl 
            "(greedy1)",  // 93 greedy 
            "(anger1)",  // 94 anger 
            "(eyes droped1)",  // 95 eyes droped 
            "(happy1)",  // 96 happy 
            "(horror1)",  // 97 horror 
            "(money1)",  // 98 money 
            "(nothing1)",  // 99 nothing 
            "(nothing to say1)",  // 100 nothing to say 
            "(cry1)",  // 101 cry 
            "(scorn1)",  // 102 scorn 
            "(secret smile1)",  // 103 secret smile 
            "(shame1)",  // 104 shame 
            "(shocked1)",  // 105 shocked 
            "(super man1)",  // 106 super man 
            "(iron man1)",  // 107 the iron man 
            "(unhappy1)",  // 108 unhappy 
            "(electric shock1)",  // 109 electric shock 
            "(beaten1)",  // 110 beaten 
            "(grin2)",  // 111 grin 
            "(happy2)",  // 112 happy 
            "(fake smile2)",  // 113 fake smile 
            "(in love2)",  // 114 in love 
            "(kiss2)",  // 115 kiss 
            "(straight face2)",  // 116 straight face 
            "(meow2)",  // 117 meaw 
            "(drunk2)",  // 118 drunk 
            "(x_x2)",  // 119 x x 
            "(kidding right2)",  // 120 youre kidding right 
            "(sweat2)",  // 122 sweat 
            "(nerd2)",  // 123 nerd 
            "(very angry2)",  // 124 very angry 
            "(disappearing2)",  // 125 disappearing 
            "(dizzy2)",  // 126 dizzy 
            "(music2)",  // 127 music 
            "(evilish)",  // 128 evilish 
            "(graffiti)",  // 129 graffiti 
            "(omg2)",  // 130 omg 
            "(on fire2)",  // 131 on fire 
            "(ouch2)",  // 132 ouch 
            "(angry2)",  // 133 angry 
            "(business2)",  // 134 serious business 
            "(sick2)",  // 135 sick 
            "(slow2)",  // 136 slow 
            "(snooty2)",  // 137 snooty 
            "(suspicious2)",  // 138 suspicious 
            "(crying2)",  // 139 crying 
            "(want2)",  // 140 want 
            "(gonna die2)",  // 141 we all gonna die 
            "(wut2)",  // 142 wut 
            "(boo2)",  // 143 boo 
            "(xd2)",  // 144 xd 
            "(kaboom2)",  // 145 kaboom 
            "(yarr2)",  // 146 yarr 
            "(ninja2)",  // 147 ninja 
            "(yuush2)",  // 148 yuush 
            "(brains2)",  // 149 brains 
            "(sleeping2)",  // 150 sleeping 
      };


        public static BitmapImage[] _emoticonImagesForList0 = null;
        public static BitmapImage[] _emoticonImagesForList1 = null;
        public static BitmapImage[] _emoticonImagesForList2 = null;

        public static int emoticon0Size = 80;
        public static int emoticon1Size = 30;
        public static int emoticon2Size = 39;

        public static void loadEmoticons()
        {

            _emoticonImagesForList0 = new BitmapImage[emoticon0Size];
            int i, j, k = 0;
            for (i = 0; i < emoticon0Size; i++)
            {
                _emoticonImagesForList0[i] = new BitmapImage();
                _emoticonImagesForList0[i].CreateOptions = BitmapCreateOptions.BackgroundCreation;
                _emoticonImagesForList0[i].UriSource = new Uri(emoticonPaths[i], UriKind.Relative);    
            }

            _emoticonImagesForList1 = new BitmapImage[emoticon1Size];
            for (j = 0; j < emoticon1Size; j++)
            {
                _emoticonImagesForList1[j] = new BitmapImage();
                _emoticonImagesForList1[j].CreateOptions = BitmapCreateOptions.BackgroundCreation;
                _emoticonImagesForList1[j].UriSource = new Uri(emoticonPaths[i+j], UriKind.Relative);
            }

            _emoticonImagesForList2 = new BitmapImage[emoticon2Size];
            for (k = 0; k < emoticon2Size; k++)
            {
                _emoticonImagesForList2[k] = new BitmapImage();
                _emoticonImagesForList2[k].CreateOptions = BitmapCreateOptions.BackgroundCreation;
                _emoticonImagesForList2[k].UriSource = new Uri(emoticonPaths[i+j+k], UriKind.Relative);
            }
        }

        //static void imageOpenedHandler(object sender, RoutedEventArgs e)
        //{
        //    BitmapImage image = (BitmapImage)sender;
        //    image.PixelHeight = 40;
        //    image.PixelWidth = 40;
        //}

        public static string[] emoticonPaths = 
        {
        	"/View/images/emoticons/emo_im_01_bigsmile.png",
			"/View/images/emoticons/emo_im_02_happy.png",
			"/View/images/emoticons/emo_im_03_laugh.png",
        	"/View/images/emoticons/emo_im_04_smile.png",
			"/View/images/emoticons/emo_im_05_wink.png",
			"/View/images/emoticons/emo_im_06_adore.png",
			"/View/images/emoticons/emo_im_07_kiss.png",
			"/View/images/emoticons/emo_im_08_kissed.png",
            "/View/images/emoticons/emo_im_09_expressionless.png",
			"/View/images/emoticons/emo_im_10_pudently.png",
			"/View/images/emoticons/emo_im_11_satisfied.png",
			"/View/images/emoticons/emo_im_12_giggle.png",
			"/View/images/emoticons/emo_im_13_impish.png",
			"/View/images/emoticons/emo_im_14_disappointment.png",
			"/View/images/emoticons/emo_im_15_beuptonogood.png",
			"/View/images/emoticons/emo_im_16_frustrated.png",
			"/View/images/emoticons/emo_im_17_sad.png",
			"/View/images/emoticons/emo_im_18_sorry.png",
			"/View/images/emoticons/emo_im_19_cry.png",
			"/View/images/emoticons/emo_im_20_boring.png",
			"/View/images/emoticons/emo_im_21_hungry.png",
			"/View/images/emoticons/emo_im_22_scared.png",
			"/View/images/emoticons/emo_im_23_shock.png",
			"/View/images/emoticons/emo_im_24_sweat.png",
			"/View/images/emoticons/emo_im_25_crying.png",
			"/View/images/emoticons/emo_im_26_lol.png",
			"/View/images/emoticons/emo_im_27_woo.png",
			"/View/images/emoticons/emo_im_28_surprise.png",
			"/View/images/emoticons/emo_im_29_frown.png",
			"/View/images/emoticons/emo_im_30_angry.png",
			"/View/images/emoticons/emo_im_31_wornout.png",
			"/View/images/emoticons/emo_im_32_stop.png",
			"/View/images/emoticons/emo_im_33_furious.png",
			"/View/images/emoticons/emo_im_34_smoking.png",
			"/View/images/emoticons/emo_im_35_hysterical.png",
			"/View/images/emoticons/emo_im_36_exclamation.png",
			"/View/images/emoticons/emo_im_37_question.png",
			"/View/images/emoticons/emo_im_38_sleep.png",
			"/View/images/emoticons/emo_im_39_aggressive.png",
			"/View/images/emoticons/emo_im_40_badly.png",
			"/View/images/emoticons/emo_im_41_singing.png",
			"/View/images/emoticons/emo_im_42_bomb.png",
			"/View/images/emoticons/emo_im_43_beaten.png",
			"/View/images/emoticons/emo_im_44_thumbsdown.png",
			"/View/images/emoticons/emo_im_45_thumbsup.png",
			"/View/images/emoticons/emo_im_46_beer.png",
			"/View/images/emoticons/emo_im_47_call.png",
			"/View/images/emoticons/emo_im_48_hi.png",
			"/View/images/emoticons/emo_im_49_hug.png",
			"/View/images/emoticons/emo_im_50_facepalm.png",
			"/View/images/emoticons/emo_im_51_easymoney.png",
			"/View/images/emoticons/emo_im_52_dizzy.png",
			"/View/images/emoticons/emo_im_53_disgust.png",
			"/View/images/emoticons/emo_im_54_cocktail.png",
			"/View/images/emoticons/emo_im_55_coffee.png",
            "/View/images/emoticons/emo_im_56_cold.png",
			"/View/images/emoticons/emo_im_57_cool.png",
			"/View/images/emoticons/emo_im_58_despair.png",
			"/View/images/emoticons/emo_im_59_hypnotic.png",
			"/View/images/emoticons/emo_im_60_stars.png",
			"/View/images/emoticons/emo_im_61_idea.png",
			"/View/images/emoticons/emo_im_62_monocle.png",
			"/View/images/emoticons/emo_im_63_movie.png",
			"/View/images/emoticons/emo_im_64_music.png",
			"/View/images/emoticons/emo_im_65_nerd.png",
			"/View/images/emoticons/emo_im_66_ninja.png",
			"/View/images/emoticons/emo_im_67_party.png",
			"/View/images/emoticons/emo_im_68_pirate.png",
			"/View/images/emoticons/emo_im_69_rage.png",
			"/View/images/emoticons/emo_im_70_rose.png",
			"/View/images/emoticons/emo_im_71_sick.png",
			"/View/images/emoticons/emo_im_72_snotty.png",
			"/View/images/emoticons/emo_im_73_stressed.png",
			"/View/images/emoticons/emo_im_74_struggle.png",
			"/View/images/emoticons/emo_im_75_study.png",
			"/View/images/emoticons/emo_im_76_sweetangel.png",
			"/View/images/emoticons/emo_im_77_thinking.png",
			"/View/images/emoticons/emo_im_78_waiting.png",
			"/View/images/emoticons/emo_im_79_whistling.png",
			"/View/images/emoticons/emo_im_80_yawn.png",

            "/View/images/emoticons/emo_im_81_exciting.png",
			"/View/images/emoticons/emo_im_82_big_smile.png",
			"/View/images/emoticons/emo_im_83_haha.png",
			"/View/images/emoticons/emo_im_84_victory.png",
			"/View/images/emoticons/emo_im_85_red_heart.png",
			"/View/images/emoticons/emo_im_86_amazing.png",
			"/View/images/emoticons/emo_im_87_black_heart.png",
			"/View/images/emoticons/emo_im_88_what.png",
			"/View/images/emoticons/emo_im_89_bad_smile.png",
			"/View/images/emoticons/emo_im_90_bad_egg.png",
			"/View/images/emoticons/emo_im_91_grimace.png",
			"/View/images/emoticons/emo_im_92_girl.png",
			"/View/images/emoticons/emo_im_93_greedy.png",
			"/View/images/emoticons/emo_im_94_anger.png",
			"/View/images/emoticons/emo_im_95_eyes_droped.png",
			"/View/images/emoticons/emo_im_96_happy.png",
			"/View/images/emoticons/emo_im_97_horror.png",
			"/View/images/emoticons/emo_im_98_money.png",
			"/View/images/emoticons/emo_im_99_nothing.png",
			"/View/images/emoticons/emo_im_100_nothing_to_say.png",
			"/View/images/emoticons/emo_im_101_cry.png",
			"/View/images/emoticons/emo_im_102_scorn.png",
			"/View/images/emoticons/emo_im_103_secret_smile.png",
			"/View/images/emoticons/emo_im_104_shame.png",
			"/View/images/emoticons/emo_im_105_shocked.png",
			"/View/images/emoticons/emo_im_106_super_man.png",
			"/View/images/emoticons/emo_im_107_the_iron_man.png",
			"/View/images/emoticons/emo_im_108_unhappy.png",
			"/View/images/emoticons/emo_im_109_electric_shock.png",
			"/View/images/emoticons/emo_im_110_beaten.png",
        
            "/View/images/emoticons/emo_im_111_grin.png",
			"/View/images/emoticons/emo_im_112_happy.png",
			"/View/images/emoticons/emo_im_113_fake_smile.png",
			"/View/images/emoticons/emo_im_114_in_love.png",
			"/View/images/emoticons/emo_im_115_kiss.png",
			"/View/images/emoticons/emo_im_116_straight_face.png",
			"/View/images/emoticons/emo_im_117_meaw.png",
			"/View/images/emoticons/emo_im_118_drunk.png",
			"/View/images/emoticons/emo_im_119_x_x.png",
			"/View/images/emoticons/emo_im_120_youre_kidding_right.png",
			"/View/images/emoticons/emo_im_122_sweat.png",
			"/View/images/emoticons/emo_im_123_nerd.png",
			"/View/images/emoticons/emo_im_124_angry.png",
			"/View/images/emoticons/emo_im_125_disappearing.png",
			"/View/images/emoticons/emo_im_126_dizzy.png",
			"/View/images/emoticons/emo_im_127_music.png",
			"/View/images/emoticons/emo_im_128_evilish.png",
			"/View/images/emoticons/emo_im_129_graffiti.png",
			"/View/images/emoticons/emo_im_130_omg.png",
			"/View/images/emoticons/emo_im_131_on_fire.png",
			"/View/images/emoticons/emo_im_132_ouch.png",
			"/View/images/emoticons/emo_im_133_angry.png",
			"/View/images/emoticons/emo_im_134_serious_business.png",
			"/View/images/emoticons/emo_im_135_sick.png",
			"/View/images/emoticons/emo_im_136_slow.png",
			"/View/images/emoticons/emo_im_137_snooty.png",
			"/View/images/emoticons/emo_im_138_suspicious.png",
			"/View/images/emoticons/emo_im_139_crying.png",
			"/View/images/emoticons/emo_im_140_want.png",
			"/View/images/emoticons/emo_im_141_we_all_gonna_die.png",
			"/View/images/emoticons/emo_im_142_wut.png",
			"/View/images/emoticons/emo_im_143_boo.png",
			"/View/images/emoticons/emo_im_144_xd.png",
			"/View/images/emoticons/emo_im_145_kaboom.png",
			"/View/images/emoticons/emo_im_146_yarr.png",
			"/View/images/emoticons/emo_im_147_ninja.png",
			"/View/images/emoticons/emo_im_148_yuush.png",
			"/View/images/emoticons/emo_im_149_brains.png",
			"/View/images/emoticons/emo_im_150_sleeping",
        };


        private static Regex _pattern;
        public static Regex Pattern
        {
            get
            {
                if (_pattern == null)
                    _pattern = createPattern();
                return _pattern;
            }
        }


        public static BitmapImage lookUpFromCache(string emoticon)
        {
            int imgIndex;
            SmileyParser.EmoticonUriHash.TryGetValue(emoticon, out imgIndex);

            if (imgIndex < _emoticonImagesForList0.Length)
            {
                return _emoticonImagesForList0[imgIndex];
            }
            else if (imgIndex < _emoticonImagesForList0.Length + _emoticonImagesForList1.Length)
            {
                return _emoticonImagesForList0[imgIndex - _emoticonImagesForList0.Length];
            }
            else 
            {
                return _emoticonImagesForList0[imgIndex - _emoticonImagesForList0.Length - _emoticonImagesForList1.Length];
            }
        }


        private static Dictionary<string, int> _emoticonUriHash;
        public static Dictionary<string, int> EmoticonUriHash
        {
            get
            {
                if (_emoticonUriHash == null)
                {
                    int i = 0;
                    _emoticonUriHash = new Dictionary<string, int>();
                    for (i = 0; i < emoticonStrings.Length; i++)
                    {
                        _emoticonUriHash.Add(emoticonStrings[i], i);
                    }
                }
                return _emoticonUriHash;
            }
        }


        private static Regex _smileyPattern;
        public static Regex SmileyPattern
        {
            get
            {
                if (_smileyPattern == null)
                    _smileyPattern = createPattern();
                return _smileyPattern;
            }
        }

        //these characters shoule be escaped for regex
        // \, *, +, ?, |, {, [, (,), ^, $,., #

        private static Regex createPattern()
        {
            StringBuilder patternString = new StringBuilder();
            int i = 0;
            //last two emoticons use '|' which needs to be escaped. 

            for (i = 0; i < emoticonStrings.Length; i++)
            {
                if (i == 8 || i == 55)
                {
                    patternString.Append(emoticonStrings[i].Replace("|", "\\|"));
                }
                else if (i == 13)
                {
                    patternString.Append(emoticonStrings[i].Replace("\\", "\\\\"));
                }
                else
                {
                    patternString.Append(emoticonStrings[i]);
                }
                patternString.Append('|');
            }
            //patternString.Replace("\\", "\\\\");

            //patternString.Append(emoticonStrings[i].Replace("|", "\\|")).Append('|').Append(emoticonStrings[i + 1].Replace("|", "\\|"));

            patternString.Replace(")", "\\)");
            patternString.Replace("(", "\\(");
            patternString.Replace("[", "\\[");
            patternString.Replace("*", "\\*");
            patternString.Replace(".", "\\.");
            patternString.Replace("^", "\\^");
            patternString.Replace("?", "\\?");
            patternString.Replace("$", "\\$");

            patternString.Replace('|', ')', patternString.Length - 1, 1);
            return new Regex("(" + patternString.ToString());
        }

        public static MatchCollection matchPattern(string messageString)
        {
            return SmileyPattern.Matches(messageString);
        }
    }
}
