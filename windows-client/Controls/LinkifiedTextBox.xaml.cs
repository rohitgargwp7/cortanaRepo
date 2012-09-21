using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using windows_client.utils;
using System.Windows.Media;
using System.Text.RegularExpressions;

namespace windows_client.Controls
{
    public partial class LinkifiedTextBox : UserControl
    {
        #region Text Property
        private bool forConversationList = false;
        public static DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(LinkifiedTextBox),
            new PropertyMetadata("", ChangedText));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        private static void ChangedText(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LinkifiedTextBox)d).ChangedText(e);
        }

        private void ChangedText(DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != e.NewValue)
            {
                Paragraph richtext = Linkify((string)e.NewValue);
                RichText.Blocks.Add(richtext);
            }
        }
        #endregion

        #region FontColor
        public static DependencyProperty ForegroundColorProperty =
        DependencyProperty.Register("ForegroundColor", typeof(SolidColorBrush), typeof(LinkifiedTextBox),
        new PropertyMetadata(ChangedForeground));

        public SolidColorBrush ForegroundColor
        {
            get { return (SolidColorBrush)GetValue(ForegroundColorProperty); }
            set { SetValue(ForegroundColorProperty, value); }
        }

        private static void ChangedForeground(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LinkifiedTextBox)d).ChangedForeground(e);
        }

        private void ChangedForeground(DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != e.NewValue)
            {
                this.RichText.Foreground = (SolidColorBrush)e.NewValue;
            }
        }
        #endregion

        public LinkifiedTextBox()
        {
            InitializeComponent();
            forConversationList = true;
        }

        public LinkifiedTextBox(int fontSize)
        {
            InitializeComponent();
            this.RichText.FontSize = fontSize;
        }

        public LinkifiedTextBox(SolidColorBrush foreground, int fontSize)
        {
            InitializeComponent();
            this.RichText.Foreground = foreground;
            this.RichText.FontSize = fontSize;
        }

        public LinkifiedTextBox(SolidColorBrush foreground)
        {
            InitializeComponent();
            this.RichText.Foreground = foreground;
        }

        private Paragraph Linkify(string message)
        {
            MatchCollection matchCollection = SmileyParser.SmileyPattern.Matches(message);
            var p = new Paragraph();
            int startIndex = 0;
            int endIndex = -1;
            int maxCount = matchCollection.Count < HikeConstants.MAX_EMOTICON_SUPPORTED ? matchCollection.Count : HikeConstants.MAX_EMOTICON_SUPPORTED;

            for (int i = 0; i < maxCount; i++)
            {
                string emoticon = matchCollection[i].ToString();

                //Regex never returns an empty string. Still have added an extra check
                if (string.IsNullOrEmpty(emoticon))
                    continue;

                int index = matchCollection[i].Index;
                endIndex = index - 1;

                if (index > 0)
                {
                    Run r = new Run();
                    r.Text = message.Substring(startIndex, endIndex - startIndex + 1);
                    p.Inlines.Add(r);
                }

                startIndex = index + emoticon.Length;

                //TODO check if imgPath is null or not
                Image img = new Image();
                img.Source = SmileyParser.lookUpFromCache(emoticon);
                if (forConversationList)
                {
                    img.Height = 25;
                    img.Width = 25;
                }
                else
                {
                    img.Height = 40;
                    img.Width = 40;
                }
                InlineUIContainer ui = new InlineUIContainer();
                ui.Child = img;
                p.Inlines.Add(ui);
            }
            if (startIndex < message.Length)
            {
                Run r2 = new Run();
                r2.Text = message.Substring(startIndex, message.Length - startIndex);
                p.Inlines.Add(r2);
            }
            return p;
        }



    }
}
