using System;
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

namespace windows_client
{
    public class SmileyParser
    {
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
        ":\'-(",  // 19 cry 
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
        "(\\_/)",  // 54 cocktail 
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
        private static Regex createPattern()
        {
            StringBuilder patternString = new StringBuilder();
            patternString.Append('(');
            for (int i = 0; i < emoticonStrings.Length; i++)
            {
                patternString.Append(emoticonStrings[i]);
                patternString.Append('|');
            }
            patternString.Append(')');
            Regex regex = new Regex(patternString.ToString());
            return regex;
        }
    }

}
