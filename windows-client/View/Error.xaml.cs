using System;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;

namespace windows_client.View
{
    public partial class Error : PhoneApplicationPage
    {
        public static Exception Exception;       
        public Error()
        {
            InitializeComponent();
        }

        // Executes when the user navigates to this page.
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ErrorText.Text = Exception.ToString();
        }
    }
}