using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using windows_client.utils;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System;
using Microsoft.Phone.Tasks;

namespace windows_client.Controls
{
    public partial class LinkifiedTextBoxConvList : UserControl
    {
        #region Text Property
        public static DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(LinkifiedTextBoxConvList),
            new PropertyMetadata("", ChangedText));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        private static void ChangedText(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LinkifiedTextBoxConvList)d).ChangedText(e);
        }

        private void ChangedText(DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != e.NewValue)
            {
                Paragraph richtext = SmileyParser.Instance.LinkifyAll((string)e.NewValue);
                RichText.Blocks.Add(richtext);
            }
        }
        #endregion

        #region FontColor
        public static DependencyProperty ForegroundColorProperty =
        DependencyProperty.Register("ForegroundColor", typeof(SolidColorBrush), typeof(LinkifiedTextBoxConvList),
            new PropertyMetadata(ChangedForeground));

        public SolidColorBrush ForegroundColor
        {
            get { return (SolidColorBrush)GetValue(ForegroundColorProperty); }
            set { SetValue(ForegroundColorProperty, value); }
        }

        private static void ChangedForeground(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LinkifiedTextBoxConvList)d).ChangedForeground(e);
        }

        private void ChangedForeground(DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != e.NewValue)
            {
                this.RichText.Foreground = (SolidColorBrush)e.NewValue;
            }
        }
        #endregion

        public LinkifiedTextBoxConvList()
        {
            InitializeComponent();
        }

        //public LinkifiedTextBoxConvList(int fontSize, string text)
        //{
        //    InitializeComponent();
        //    this.RichText.FontSize = fontSize;
        //    this.Text = text;
        //    Paragraph richtext = SmileyParser.Instance.LinkifyAll(this.Text);
        //    RichText.Blocks.Add(richtext);
        //}

        //public LinkifiedTextBoxConvList(SolidColorBrush foreground, int fontSize, string text)
        //{
        //    InitializeComponent();
        //    this.RichText.Foreground = foreground;
        //    this.RichText.FontSize = fontSize;
        //    this.Text = text;
        //    Paragraph richtext = SmileyParser.Instance.LinkifyAll(this.Text);
        //    RichText.Blocks.Add(richtext);
        //}

        //public LinkifiedTextBox(SolidColorBrush foreground, string text)
        //{
        //    InitializeComponent();
        //    this.RichText.Foreground = foreground;
        //    this.Text = text;
        //    Paragraph richtext = SmileyParser.Instance.LinkifyAll(this.Text);
        //    RichText.Blocks.Add(richtext);
        //}
    }
}
