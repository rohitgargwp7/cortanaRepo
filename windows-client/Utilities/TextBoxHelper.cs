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
using System.Text.RegularExpressions;

namespace windows_client.Utilities
{
    public class TextBoxHelper
    {
        public static Paragraph Linkify(string message)
        {
            MatchCollection matchCollection = SmileyParser.SmileyPattern.Matches(message);
            var p = new Paragraph();
            int startIndex = 0;
            int endIndex = -1;

            for (int i = 0; i < matchCollection.Count; i++)
            {
                String emoticon = matchCollection[i].ToString();

                //Regex never returns an empty string. Still have added an extra check
                if (String.IsNullOrEmpty(emoticon))
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
                img.Height = 40;
                img.Width = 40;

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
