using System;
using System.Windows;
using Microsoft.Phone.Controls;
using windows_client.utils;
using Newtonsoft.Json.Linq;
using System.IO.IsolatedStorage;
using System.Windows.Media;
using Microsoft.Phone.Shell;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using windows_client.Languages;
using System.Windows.Controls;

namespace windows_client
{
    public partial class EnterNumber : PhoneApplicationPage
    {
        string phoneNumber;
        private ApplicationBar appBar;
        ApplicationBarIconButton nextIconButton;
        private string countryCode;
        bool isGroupViewOpened = false;
        private Dictionary<string, string> isoCodeCountryCode = new Dictionary<string, string>();

        public class Group<T> : List<T>
        {
            public Group(string name, List<T> items)
            {
                this.Title = name;
            }

            public string Title
            {
                get;
                set;
            }

        }

        public EnterNumber()
        {
            InitializeComponent();
            //this.Loaded += new RoutedEventHandler(EnterNumberPage_Loaded);

            initializeCountryCodes();

            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.Opacity = 1;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = false;

            nextIconButton = new ApplicationBarIconButton();
            nextIconButton.IconUri = new Uri("/View/images/icon_next.png", UriKind.Relative);
            nextIconButton.Text = AppResources.AppBar_Next_Btn;
            nextIconButton.Click += new EventHandler(enterPhoneBtn_Click);
            nextIconButton.IsEnabled = false;
            appBar.Buttons.Add(nextIconButton);
            enterNumber.ApplicationBar = appBar;
            this.countryList.ItemsSource = GetGroupedList();
        }

        private void initializeCountryCodes()
        {
            isoCodeCountryCode.Add("AF", "Afghanistan +93");
            isoCodeCountryCode.Add("AL", "Albania +355");
            isoCodeCountryCode.Add("DZ", "Algeria +213");
            isoCodeCountryCode.Add("AS", "American Samoa +1684");
            isoCodeCountryCode.Add("AD", "Andorra +376");
            isoCodeCountryCode.Add("AO", "Angola +244");
            isoCodeCountryCode.Add("AI", "Anguilla +1264");
            isoCodeCountryCode.Add("AQ", "Antarctica +672");
            isoCodeCountryCode.Add("AG", "Antigua and Barbuda +1268");
            isoCodeCountryCode.Add("AR", "Argentina +54");
            isoCodeCountryCode.Add("AM", "Armenia +374");
            isoCodeCountryCode.Add("AW", "Aruba +297");
            isoCodeCountryCode.Add("AU", "Australia +61");
            isoCodeCountryCode.Add("AT", "Austria +43");
            isoCodeCountryCode.Add("AZ", "Azerbaijan +994");
            isoCodeCountryCode.Add("BS", "Bahamas +1242");
            isoCodeCountryCode.Add("BH", "Bahrain +973");
            isoCodeCountryCode.Add("BD", "Bangladesh +880");
            isoCodeCountryCode.Add("BB", "Barbados +1246");
            isoCodeCountryCode.Add("BY", "Belarus +375");
            isoCodeCountryCode.Add("BE", "Belgium +32");
            isoCodeCountryCode.Add("BZ", "Belize +501");
            isoCodeCountryCode.Add("BJ", "Benin +229");
            isoCodeCountryCode.Add("BM", "Bermuda +1441");
            isoCodeCountryCode.Add("BT", "Bhutan +975");
            isoCodeCountryCode.Add("BO", "Bolivia +591");
            isoCodeCountryCode.Add("BA", "Bosnia and Herzegovina +387");
            isoCodeCountryCode.Add("BW", "Botswana +267");
            isoCodeCountryCode.Add("BR", "Brazil +55");
            isoCodeCountryCode.Add("VG", "British Virgin Islands +1284");
            isoCodeCountryCode.Add("BN", "Brunei +673");
            isoCodeCountryCode.Add("BG", "Bulgaria +359");
            isoCodeCountryCode.Add("BF", "Burkina Faso +226");
            isoCodeCountryCode.Add("MM", "Burma (Myanmar) +95");
            isoCodeCountryCode.Add("BI", "Burundi +257");
            isoCodeCountryCode.Add("KH", "Cambodia +855");
            isoCodeCountryCode.Add("CM", "Cameroon +237");
            isoCodeCountryCode.Add("CA", "Canada +1");
            isoCodeCountryCode.Add("CV", "Cape Verde +238");
            isoCodeCountryCode.Add("KY", "Cayman Islands +1345");
            isoCodeCountryCode.Add("CF", "Central African Republic +236");
            isoCodeCountryCode.Add("TD", "Chad +235");
            isoCodeCountryCode.Add("CL", "Chile +56");
            isoCodeCountryCode.Add("CN", "China +86");
            isoCodeCountryCode.Add("CX", "Christmas Island +61");
            isoCodeCountryCode.Add("CC", "Cocos (Keeling) Islands +61");
            isoCodeCountryCode.Add("CO", "Colombia +57");
            isoCodeCountryCode.Add("KM", "Comoros +269");
            isoCodeCountryCode.Add("CG", "Republic of the Congo +242");
            isoCodeCountryCode.Add("CD", "Democratic Republic of the Congo +243");
            isoCodeCountryCode.Add("CK", "Cook Islands +682");
            isoCodeCountryCode.Add("CR", "Costa Rica +506");
            isoCodeCountryCode.Add("HR", "Croatia +385");
            isoCodeCountryCode.Add("CU", "Cuba +53");
            isoCodeCountryCode.Add("CY", "Cyprus +357");
            isoCodeCountryCode.Add("CZ", "Czech Republic +420");
            isoCodeCountryCode.Add("DK", "Denmark +45");
            isoCodeCountryCode.Add("DJ", "Djibouti +253");
            isoCodeCountryCode.Add("DM", "Dominica +1767");
            isoCodeCountryCode.Add("DO", "Dominican Republic +1809");
            isoCodeCountryCode.Add("TL", "Timor-Leste +670");
            isoCodeCountryCode.Add("EC", "Ecuador +593");
            isoCodeCountryCode.Add("EG", "Egypt +20");
            isoCodeCountryCode.Add("SV", "El Salvador +503");
            isoCodeCountryCode.Add("GQ", "Equatorial Guinea +240");
            isoCodeCountryCode.Add("ER", "Eritrea +291");
            isoCodeCountryCode.Add("EE", "Estonia +372");
            isoCodeCountryCode.Add("ET", "Ethiopia +251");
            isoCodeCountryCode.Add("FK", "Falkland Islands +500");
            isoCodeCountryCode.Add("FO", "Faroe Islands +298");
            isoCodeCountryCode.Add("FJ", "Fiji +679");
            isoCodeCountryCode.Add("FI", "Finland +358");
            isoCodeCountryCode.Add("FR", "France +33");
            isoCodeCountryCode.Add("PF", "French Polynesia +689");
            isoCodeCountryCode.Add("GA", "Gabon +241");
            isoCodeCountryCode.Add("GM", "Gambia +220");
            isoCodeCountryCode.Add("GZ", "Gaza Strip +970");
            isoCodeCountryCode.Add("GE", "Georgia +995");
            isoCodeCountryCode.Add("DE", "Germany +49");
            isoCodeCountryCode.Add("GH", "Ghana +233");
            isoCodeCountryCode.Add("GI", "Gibraltar +350");
            isoCodeCountryCode.Add("GR", "Greece +30");
            isoCodeCountryCode.Add("GL", "Greenland +299");
            isoCodeCountryCode.Add("GD", "Grenada +1473");
            isoCodeCountryCode.Add("GU", "Guam +1671");
            isoCodeCountryCode.Add("GT", "Guatemala +502");
            isoCodeCountryCode.Add("GN", "Guinea +224");
            isoCodeCountryCode.Add("GW", "Guinea-Bissau +245");
            isoCodeCountryCode.Add("GY", "Guyana +592");
            isoCodeCountryCode.Add("HT", "Haiti +509");
            isoCodeCountryCode.Add("HN", "Honduras +504");
            isoCodeCountryCode.Add("HK", "Hong Kong +852");
            isoCodeCountryCode.Add("HU", "Hungary +36");
            isoCodeCountryCode.Add("IS", "Iceland +354");
            isoCodeCountryCode.Add("IN", "India +91");
            isoCodeCountryCode.Add("ID", "Indonesia +62");
            isoCodeCountryCode.Add("IR", "Iran +98");
            isoCodeCountryCode.Add("IQ", "Iraq +964");
            isoCodeCountryCode.Add("IE", "Ireland +353");
            isoCodeCountryCode.Add("IM", "Isle of Man +44");
            isoCodeCountryCode.Add("IL", "Israel +972");
            isoCodeCountryCode.Add("IT", "Italy +39");
            isoCodeCountryCode.Add("CI", "Ivory Coast +225");
            isoCodeCountryCode.Add("JM", "Jamaica +1876");
            isoCodeCountryCode.Add("JP", "Japan +81");
            isoCodeCountryCode.Add("JO", "Jordan +962");
            isoCodeCountryCode.Add("KZ", "Kazakhstan +7");
            isoCodeCountryCode.Add("KE", "Kenya +254");
            isoCodeCountryCode.Add("KI", "Kiribati +686");
            isoCodeCountryCode.Add("KO", "Kosovo +381");
            isoCodeCountryCode.Add("KW", "Kuwait +965");
            isoCodeCountryCode.Add("KG", "Kyrgyzstan +996");
            isoCodeCountryCode.Add("LA", "Laos +856");
            isoCodeCountryCode.Add("LV", "Latvia +371");
            isoCodeCountryCode.Add("LB", "Lebanon +961");
            isoCodeCountryCode.Add("LS", "Lesotho +266");
            isoCodeCountryCode.Add("LR", "Liberia +231");
            isoCodeCountryCode.Add("LY", "Libya +218");
            isoCodeCountryCode.Add("LI", "Liechtenstein +423");
            isoCodeCountryCode.Add("LT", "Lithuania +370");
            isoCodeCountryCode.Add("LU", "Luxembourg +352");
            isoCodeCountryCode.Add("MO", "Macau +853");
            isoCodeCountryCode.Add("MK", "Macedonia +389");
            isoCodeCountryCode.Add("MG", "Madagascar +261");
            isoCodeCountryCode.Add("MW", "Malawi +265");
            isoCodeCountryCode.Add("MY", "Malaysia +60");
            isoCodeCountryCode.Add("MV", "Maldives +960");
            isoCodeCountryCode.Add("ML", "Mali +223");
            isoCodeCountryCode.Add("MT", "Malta +356");
            isoCodeCountryCode.Add("MH", "Marshall Islands +692");
            isoCodeCountryCode.Add("MR", "Mauritania +222");
            isoCodeCountryCode.Add("MU", "Mauritius +230");
            isoCodeCountryCode.Add("YT", "Mayotte +262");
            isoCodeCountryCode.Add("MX", "Mexico +52");
            isoCodeCountryCode.Add("FM", "Micronesia +691");
            isoCodeCountryCode.Add("MD", "Moldova +373");
            isoCodeCountryCode.Add("MC", "Monaco +377");
            isoCodeCountryCode.Add("MN", "Mongolia +976");
            isoCodeCountryCode.Add("ME", "Montenegro +382");
            isoCodeCountryCode.Add("MS", "Montserrat +1664");
            isoCodeCountryCode.Add("MA", "Morocco +212");
            isoCodeCountryCode.Add("MZ", "Mozambique +258");
            isoCodeCountryCode.Add("NA", "Namibia +264");
            isoCodeCountryCode.Add("NR", "Nauru +674");
            isoCodeCountryCode.Add("NP", "Nepal +977");
            isoCodeCountryCode.Add("NL", "Netherlands +31");
            isoCodeCountryCode.Add("AN", "Netherlands Antilles +599");
            isoCodeCountryCode.Add("NC", "New Caledonia +687");
            isoCodeCountryCode.Add("NZ", "New Zealand +64");
            isoCodeCountryCode.Add("NI", "Nicaragua +505");
            isoCodeCountryCode.Add("NE", "Niger +227");
            isoCodeCountryCode.Add("NG", "Nigeria +234");
            isoCodeCountryCode.Add("NU", "Niue +683");
            isoCodeCountryCode.Add("NFK", "Norfolk Island +672");
            isoCodeCountryCode.Add("MP", "Northern Mariana Islands +1670");
            isoCodeCountryCode.Add("KP", "North Korea +850");
            isoCodeCountryCode.Add("NO", "Norway +47");
            isoCodeCountryCode.Add("OM", "Oman +968");
            isoCodeCountryCode.Add("PK", "Pakistan +92");
            isoCodeCountryCode.Add("PW", "Palau +680");
            isoCodeCountryCode.Add("PA", "Panama +507");
            isoCodeCountryCode.Add("PG", "Papua New Guinea +675");
            isoCodeCountryCode.Add("PY", "Paraguay +595");
            isoCodeCountryCode.Add("PE", "Peru +51");
            isoCodeCountryCode.Add("PH", "Philippines +63");
            isoCodeCountryCode.Add("PN", "Pitcairn Islands +870");
            isoCodeCountryCode.Add("PL", "Poland +48");
            isoCodeCountryCode.Add("PT", "Portugal +351");
            isoCodeCountryCode.Add("PR", "Puerto Rico +1");
            isoCodeCountryCode.Add("QA", "Qatar +974");
            isoCodeCountryCode.Add("RO", "Romania +40");
            isoCodeCountryCode.Add("RU", "Russia +7");
            isoCodeCountryCode.Add("RW", "Rwanda +250");
            isoCodeCountryCode.Add("BL", "Saint Barthelemy +590");
            isoCodeCountryCode.Add("WS", "Samoa +685");
            isoCodeCountryCode.Add("SM", "San Marino +378");
            isoCodeCountryCode.Add("ST", "Sao Tome and Principe +239");
            isoCodeCountryCode.Add("SA", "Saudi Arabia +966");
            isoCodeCountryCode.Add("SN", "Senegal +221");
            isoCodeCountryCode.Add("RS", "Serbia +381");
            isoCodeCountryCode.Add("SC", "Seychelles +248");
            isoCodeCountryCode.Add("SL", "Sierra Leone +232");
            isoCodeCountryCode.Add("SG", "Singapore +65");
            isoCodeCountryCode.Add("SK", "Slovakia +421");
            isoCodeCountryCode.Add("SI", "Slovenia +386");
            isoCodeCountryCode.Add("SB", "Solomon Islands +677");
            isoCodeCountryCode.Add("SO", "Somalia +252");
            isoCodeCountryCode.Add("ZA", "South Africa +27");
            isoCodeCountryCode.Add("KR", "South Korea +82");
            isoCodeCountryCode.Add("ES", "Spain +34");
            isoCodeCountryCode.Add("LK", "Sri Lanka +94");
            isoCodeCountryCode.Add("SH", "Saint Helena +290");
            isoCodeCountryCode.Add("KN", "Saint Kitts and Nevis +1869");
            isoCodeCountryCode.Add("LC", "Saint Lucia +1758");
            isoCodeCountryCode.Add("MF", "Saint Martin +1599");
            isoCodeCountryCode.Add("PM", "Saint Pierre and Miquelon +508");
            isoCodeCountryCode.Add("VC", "Saint Vincent and the Grenadines +1784");
            isoCodeCountryCode.Add("SD", "Sudan +249");
            isoCodeCountryCode.Add("SR", "Suriname +597");
            isoCodeCountryCode.Add("SZ", "Swaziland +268");
            isoCodeCountryCode.Add("SE", "Sweden +46");
            isoCodeCountryCode.Add("CH", "Switzerland +41");
            isoCodeCountryCode.Add("SY", "Syria +963");
            isoCodeCountryCode.Add("TW", "Taiwan +886");
            isoCodeCountryCode.Add("TJ", "Tajikistan +992");
            isoCodeCountryCode.Add("TZ", "Tanzania +255");
            isoCodeCountryCode.Add("TH", "Thailand +66");
            isoCodeCountryCode.Add("TG", "Togo +228");
            isoCodeCountryCode.Add("TK", "Tokelau +690");
            isoCodeCountryCode.Add("TO", "Tonga +676");
            isoCodeCountryCode.Add("TT", "Trinidad and Tobago +1868");
            isoCodeCountryCode.Add("TN", "Tunisia +216");
            isoCodeCountryCode.Add("TR", "Turkey +90");
            isoCodeCountryCode.Add("TM", "Turkmenistan +993");
            isoCodeCountryCode.Add("TC", "Turks and Caicos Islands +1649");
            isoCodeCountryCode.Add("TV", "Tuvalu +688");
            isoCodeCountryCode.Add("AE", "United Arab Emirates +971");
            isoCodeCountryCode.Add("UG", "Uganda +256");
            isoCodeCountryCode.Add("GB", "United Kingdom +44");
            isoCodeCountryCode.Add("UA", "Ukraine +380");
            isoCodeCountryCode.Add("UY", "Uruguay +598");
            isoCodeCountryCode.Add("US", "United States +1");
            isoCodeCountryCode.Add("UZ", "Uzbekistan +998");
            isoCodeCountryCode.Add("VU", "Vanuatu +678");
            isoCodeCountryCode.Add("VA", "Holy See (Vatican City) +39");
            isoCodeCountryCode.Add("VE", "Venezuela +58");
            isoCodeCountryCode.Add("VN", "Vietnam +84");
            isoCodeCountryCode.Add("VI", "US Virgin Islands +1340");
            isoCodeCountryCode.Add("WF", "Wallis and Futuna +681");
            isoCodeCountryCode.Add("WB", "West Bank +970");
            isoCodeCountryCode.Add("YE", "Yemen +967");
            isoCodeCountryCode.Add("ZM", "Zambia +260");
            isoCodeCountryCode.Add("ZW", "Zimbabwe +263");

        }

        private void enterPhoneBtn_Click(object sender, EventArgs e)
        {
            phoneNumber = countryCode.Substring(countryCode.IndexOf('+')) + txtEnterPhone.Text.Trim();
            if (String.IsNullOrEmpty(phoneNumber))
                return;
            if (phoneNumber.Length < 1 || phoneNumber.Length > 15)
            {
                MessageBox.Show(AppResources.EnterNumber_MsgBoxText_Msg, AppResources.EnterNumber_IncorrectPh_TxtBlk, MessageBoxButton.OK);
                txtEnterPhone.Select(txtEnterPhone.Text.Length, 0);
                return;
            }
            if (!NetworkInterface.GetIsNetworkAvailable()) // if no network
            {
                progressBar.Opacity = 0;
                progressBar.IsEnabled = false;
                msisdnErrorTxt.Text = AppResources.Connectivity_Issue;
                msisdnErrorTxt.Visibility = Visibility.Visible;
                return;
            }
            MessageBoxResult res = MessageBox.Show(phoneNumber, AppResources.EnterMsisdn_ConfirmNumber_Txt, MessageBoxButton.OKCancel);
            if (res == MessageBoxResult.Cancel)
                return;
            txtEnterPhone.IsReadOnly = true;
            nextIconButton.IsEnabled = false;
            msgTxtBlk.Opacity = 1;
            msgTxtBlk.Text = AppResources.EnterNumber_VerifyNumberMsg_TxtBlk;
            msisdnErrorTxt.Visibility = Visibility.Collapsed;
            progressBar.Opacity = 1;
            progressBar.IsEnabled = true;
            AccountUtils.validateNumber(phoneNumber, new AccountUtils.postResponseFunction(msisdnPostResponse_Callback));
        }

        private void msisdnPostResponse_Callback(JObject obj)
        {
            if (obj == null)
            {
                //logger.Info("HTTP", "Unable to Validate Phone Number.");
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    msgTxtBlk.Opacity = 0;
                    msisdnErrorTxt.Visibility = Visibility.Visible;
                    progressBar.Opacity = 0;
                    progressBar.IsEnabled = false;
                    txtEnterPhone.IsReadOnly = false;

                    if (!string.IsNullOrWhiteSpace(phoneNumber))
                        nextIconButton.IsEnabled = true;
                });
                return;
            }
            string unauthedMSISDN = (string)obj[App.MSISDN_SETTING];
            if (unauthedMSISDN == null)
            {
                //logger.Info("SignupTask", "Unable to send PIN to user");
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    msgTxtBlk.Opacity = 0;
                    msisdnErrorTxt.Text = AppResources.EnterNumber_IncorrectPh_TxtBlk;
                    msisdnErrorTxt.Visibility = Visibility.Visible;
                    progressBar.Opacity = 0;
                    progressBar.IsEnabled = false;
                    txtEnterPhone.IsReadOnly = false;
                });
                return;
            }
            /*If all well*/
            App.WriteToIsoStorageSettings(App.MSISDN_SETTING, unauthedMSISDN);

            string digits = countryCode.Substring(countryCode.IndexOf('+'));
            App.WriteToIsoStorageSettings(App.COUNTRY_CODE_SETTING, countryCode.Substring(countryCode.IndexOf('+')));

            Uri nextPage = new Uri("/View/EnterPin.xaml", UriKind.Relative);
            /*This is used to avoid cross thread invokation*/
            try
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    txtEnterPhone.IsReadOnly = false;
                    PhoneApplicationService.Current.State["EnteredPhone"] = txtEnterPhone.Text;
                    NavigationService.Navigate(nextPage);
                    progressBar.Opacity = 0;
                    progressBar.IsEnabled = false;
                });
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception handled in page EnterNumber Screen : " + e.StackTrace);
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            while (NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();

            if(PhoneApplicationService.Current.State.ContainsKey(HikeConstants.COUNTRY_SELECTED))
            {
                txtEnterCountry.Text = countryCode = (string)PhoneApplicationService.Current.State[HikeConstants.COUNTRY_SELECTED];
            }
            else
            {
                string ISORegion = "";
                string countryCodeName = CultureInfo.CurrentCulture.Name;
                try
                {
                    RegionInfo reg = new RegionInfo(countryCodeName);
                    ISORegion = reg.TwoLetterISORegionName;
                }
                catch (ArgumentException argEx)
                {
                    // The country code was not valid 
                }

                if (isoCodeCountryCode.ContainsKey(ISORegion))
                {
                    txtEnterCountry.Text = countryCode = isoCodeCountryCode[ISORegion];
                }
                else
                {
                    txtEnterCountry.Text = countryCode = "India + 91";
                }
            }

            if (App.IS_TOMBSTONED) /* ****************************    HANDLING TOMBSTONE    *************************** */
            {
                object obj = null;
                if (this.State.TryGetValue("txtEnterPhone", out obj))
                {
                    txtEnterPhone.Text = (string)obj;
                    txtEnterPhone.Select(txtEnterPhone.Text.Length, 0);
                    obj = null;
                }

                if (this.State.TryGetValue("msisdnErrorTxt.Visibility", out obj))
                {
                    msisdnErrorTxt.Visibility = (Visibility)obj;
                    msisdnErrorTxt.Text = (string)this.State["msisdnErrorTxt.Text"];
                }
            }

            if (String.IsNullOrWhiteSpace(txtEnterPhone.Text))
                nextIconButton.IsEnabled = false;
            else
                nextIconButton.IsEnabled = true;
            if (PhoneApplicationService.Current.State.ContainsKey("EnteredPhone"))
            {
                txtEnterPhone.Text = (string)PhoneApplicationService.Current.State["EnteredPhone"];
                txtEnterPhone.Select(txtEnterPhone.Text.Length, 0);
                PhoneApplicationService.Current.State.Remove("EnteredPhone");
            }

            txtEnterPhone.Hint = AppResources.EnterNumber_Ph_Hint_TxtBox;
            txtEnterCountry.Foreground = UI_Utils.Instance.Black;
            
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            string uri = e.Uri.ToString();
            PhoneApplicationService.Current.State[HikeConstants.COUNTRY_SELECTED] = txtEnterCountry.Text;

            if (!uri.Contains("View"))
            {
                if (!string.IsNullOrWhiteSpace(txtEnterPhone.Text))
                    this.State["txtEnterPhone"] = txtEnterPhone.Text;
                else
                    this.State.Remove("txtEnterPhone");

                if (msisdnErrorTxt.Visibility == Visibility.Visible)
                {
                    this.State["msisdnErrorTxt.Text"] = msisdnErrorTxt.Text;
                    this.State["msisdnErrorTxt.Visibility"] = msisdnErrorTxt.Visibility;
                }
                else
                {
                    this.State.Remove("msisdnErrorTxt.Text");
                    this.State.Remove("msisdnErrorTxt.Visibility");
                }
            }
            else
                App.IS_TOMBSTONED = false;
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (isGroupViewOpened)
            {
                base.OnBackKeyPress(e);
                return;
            }
            if (countryList.Visibility == Visibility.Visible && ContentPanel.Visibility == Visibility.Collapsed)
            {
                e.Cancel = true;
                ContentPanel.Visibility = Visibility.Visible;
                countryList.Visibility = Visibility.Collapsed;
                base.OnBackKeyPress(e);
                return;
            }
            base.OnBackKeyPress(e);
            Uri nextPage = new Uri("/View/WelcomePage.xaml", UriKind.Relative);
            NavigationService.Navigate(nextPage);
        }

        void EnterNumberPage_Loaded(object sender, RoutedEventArgs e)
        {
            txtEnterPhone.Hint = AppResources.EnterNumber_Ph_Hint_TxtBox;
            txtEnterCountry.Foreground = UI_Utils.Instance.Black;
        }

        private void txtEnterPhone_GotFocus(object sender, RoutedEventArgs e)
        {
            txtEnterPhone.Hint = AppResources.EnterNumber_Ph_Hint_TxtBox;
            txtEnterPhone.Foreground = UI_Utils.Instance.SignUpForeground;
        }

        private void txtEnterPhone_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtEnterPhone.Text))
            {
                txtEnterPhone.Foreground = UI_Utils.Instance.SignUpForeground;
            }
            else
            {
                nextIconButton.IsEnabled = false;
            }
            if (txtEnterPhone.Text.Length >= 1 && txtEnterPhone.Text.Length <= 15)
                nextIconButton.IsEnabled = true;
            else
                nextIconButton.IsEnabled = false;
        }

        private void txtEnterPhone_LostFocus(object sender, RoutedEventArgs e)
        {
            this.txtEnterPhone.Background = UI_Utils.Instance.White;
        }

        private void txtEnterCountry_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {

        }

        private void txtEnterCountry_GotFocus(object sender, RoutedEventArgs e)
        {
            ContentPanel.Visibility = Visibility.Collapsed;
            countryList.Visibility = Visibility.Visible;
        }

        private void countryList_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            string selectedCountryCode = (sender as TextBlock).DataContext as string;
            txtEnterCountry.Text = countryCode = selectedCountryCode;
            txtEnterCountry.Foreground = UI_Utils.Instance.Black;
            countryList.Visibility = Visibility.Collapsed;
            ContentPanel.Visibility = Visibility.Visible;
        }

        private void txtEnterPhone_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (txtEnterPhone.Text.Length > 0)
            {
                string lastCharacter = txtEnterPhone.Text.Substring(txtEnterPhone.Text.Length - 1);
                bool isDigit = true;
                double num;
                isDigit = Double.TryParse(lastCharacter, out num);
                if (!isDigit)
                {
                    if (string.IsNullOrEmpty(txtEnterPhone.Text) || txtEnterPhone.Text.Length == 1)
                        txtEnterPhone.Text = "";
                    else
                        txtEnterPhone.Text = txtEnterPhone.Text.Substring(0, txtEnterPhone.Text.Length - 1);
                }
                txtEnterPhone.Select(txtEnterPhone.Text.Length, 0);
            }
        }

        private List<Group<string>> GetGroupedList()
        {
            List<Group<string>> glist = createGroups();
            foreach (string val in isoCodeCountryCode.Values)
            {
                string ch = GetCaptionGroup(val);
                // calculate the index into the list
                int index = (ch == "#") ? 26 : ch[0] - 'a';
                // and add the entry
                glist[index].Add(val);
            }
            return glist;
        }

        private static string GetCaptionGroup(string str)
        {
            char key = char.ToLower(str[0]);
            if (key < 'a' || key > 'z')
            {
                key = '#';
            }
            return key.ToString();
        }

        private List<Group<string>> createGroups()
        {
            string Groups = "abcdefghijklmnopqrstuvwxyz#";
            List<Group<string>> glist = new List<Group<string>>(27);
            foreach (char c in Groups)
            {
                Group<string> g = new Group<string>(c.ToString(), new List<string>(1));
                glist.Add(g);
            }
            return glist;
        }

    }
}