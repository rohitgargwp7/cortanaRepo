using System;
using System.Windows;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media.Imaging;
using System.IO;
using windows_client.DbUtils;
using System.IO.IsolatedStorage;
namespace windows_client.View
{
    public partial class DisplayImage : PhoneApplicationPage
    {
        private BitmapImage fileImage;

        double initialAngle;
        double initialScale;

        public DisplayImage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (PhoneApplicationService.Current.State.ContainsKey("objectForFileTransfer"))
            {
                object[] fileTapped = (object[])PhoneApplicationService.Current.State["objectForFileTransfer"];
                long messsageId = (long)fileTapped[0];
                string msisdn = (string)fileTapped[1];
                string filePath = HikeConstants.FILES_BYTE_LOCATION + "/" + msisdn + "/" + Convert.ToString(messsageId);

                byte[] filebytes;
                MiscDBUtil.readFileFromIsolatedStorage(filePath, out filebytes);
                MemoryStream memStream = new MemoryStream(filebytes);
                memStream.Seek(0, SeekOrigin.Begin);
                fileImage = new BitmapImage();
                fileImage.SetSource(memStream);
                this.FileImage.Source = fileImage;
            }

        }

        private void OnPinchStarted(object sender, PinchStartedGestureEventArgs e)
        {
            initialAngle = transform.Rotation;
            initialScale = transform.ScaleX;
        }

        private void OnPinchDelta(object sender, PinchGestureEventArgs e)
        {
            //transform.Rotation = initialAngle + e.TotalAngleDelta;
            transform.ScaleX = initialScale * e.DistanceRatio;
            transform.ScaleY = initialScale * e.DistanceRatio;
        }

        private void GestureListener_PinchCompleted(object sender, PinchGestureEventArgs e)
        {
            //transform.ScaleX = initialScale;
            //transform.ScaleY = initialScale;
        }

        private void GestureListener_DragDelta(object sender, DragDeltaGestureEventArgs e)
        {
            // if is not touch enabled or the scale is different than 1 then don’t allow moving
            if (transform.ScaleX <= 1.1)
                return;
            double centerX = transform.CenterX;
            double centerY = transform.CenterY;
            double translateX = transform.TranslateX;
            double translateY = transform.TranslateY;
            double scale = transform.ScaleX;
            double width = FileImage.ActualWidth;
            double height = FileImage.ActualHeight;

            // verify limits to not allow the image to get out of area

            if (centerX - scale * centerX + translateX + e.HorizontalChange < 0 &&
            centerX + scale * (width - centerX) + translateX + e.HorizontalChange > width)
            {
                transform.TranslateX += e.HorizontalChange;
            }

            if (centerY - scale * centerY + translateY + e.VerticalChange < 0 &&
            centerY + scale * (height - centerY) + translateY + e.VerticalChange > height)
            {
                transform.TranslateY += e.VerticalChange;
            }
            return;
        }
    }
}