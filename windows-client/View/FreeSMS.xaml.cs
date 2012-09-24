using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

namespace windows_client.View
{
    public partial class FreeSMS : PhoneApplicationPage
    {
        private readonly SolidColorBrush rectangleColor = new SolidColorBrush(Color.FromArgb(255, 51, 51, 51));
        private readonly Thickness box4Margin = new Thickness(5,5,5,5);

        public FreeSMS()
        {
            InitializeComponent();
            initpageBasedOnState();
        }

        private void initpageBasedOnState()
        {
            int creditsRemaining = (int)App.appSettings[App.SMS_SETTING];
//            int creditsRemaining = 8796;
            TextBlock t3 = null;
            if (creditsRemaining > 999)
            {
                ColumnDefinition c4 = new ColumnDefinition();
                c4.Width = GridLength.Auto;
                SMSCounterGrid.ColumnDefinitions.Add(c4);
                
                Rectangle r3 = new Rectangle();
                r3.Fill = rectangleColor;
                r3.Margin = box4Margin;
                r3.Width = 40;
                Grid.SetColumn(r3, 3);
                SMSCounterGrid.Children.Add(r3);

                t3 = new TextBlock();
                t3.FontSize = 45;
                t3.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                Grid.SetColumn(t3, 3);
                SMSCounterGrid.Children.Add(t3);
            }

            creditsRemaining %= 10000;
            string strCreditsWithZeroes;
            if(creditsRemaining > 999)
                strCreditsWithZeroes = creditsRemaining.ToString("0000");
            else
                strCreditsWithZeroes = creditsRemaining.ToString("000");

            t0.Text = strCreditsWithZeroes[0].ToString();
            t1.Text = strCreditsWithZeroes[1].ToString();
            t2.Text = strCreditsWithZeroes[2].ToString();
            if(t3!=null)
                t3.Text = strCreditsWithZeroes[3].ToString();

        }
    }
}