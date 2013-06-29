using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Phone.Controls;
using System.Windows.Media.Imaging;
using System.IO;
using System.Device.Location;
using Microsoft.Phone.Shell;
using Newtonsoft.Json.Linq;
using windows_client.Languages;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Services;
using Windows.Devices.Geolocation;
using System.Collections.Generic;
using System.Windows.Shapes;
using Newtonsoft.Json;
using windows_client.utils;

namespace windows_client.View
{
    public partial class ShareLocation : PhoneApplicationPage
    {
        ApplicationBarIconButton shareIconButton = null;
        ApplicationBarIconButton appBarLocateMeButton = null;
        ApplicationBarIconButton appBarPlacesButton = null;
        
        // Progress indicator shown in system tray
        private ProgressIndicator ProgressIndicator = null;

        // My current location
        private GeoCoordinate MyCoordinate = null;
        private GeoCoordinate SelectedCoordinate = null;

        /// <summary>
        /// True when this object instance has been just created, otherwise false
        /// </summary>
        private bool _isNewInstance = true;

        /// <summary>
        /// True when access to user location is allowed, otherwise false
        /// </summary>
        private bool _isLocationAllowed = false;

        /// <summary>
        /// True when route is being searched, otherwise false
        /// </summary>
        private bool _isPlacesSearch = false;

        /// <summary>
        /// Accuracy of my current location in meters;
        /// </summary>
        private double _accuracy = 0.0;

        private string nokiaPlacesUrl = "http://places.nlp.nokia.com/places/v1/discover/explore";
        private string nokiaSeacrhUrl = "http://places.nlp.nokia.com/places/v1/discover/search";
        private string nokiaPlacesAppID = "KyG8YRrgO09I7bm9jXwd";
        private string nokiaPlacesAppCode = "Uhjk-Gny7_A-ISaJb3DMKQ";
        private List<Place> Places;
        Place SelectedPlace;

        private void BuildApplicationBar()
        {
            // Set the page's ApplicationBar to a new instance of ApplicationBar.    
            ApplicationBar = new ApplicationBar();

            ApplicationBar.Mode = ApplicationBarMode.Default;
            ApplicationBar.IsVisible = true;
            ApplicationBar.Opacity = 1.0;
            ApplicationBar.IsMenuEnabled = true;

            // Create new buttons with the localized strings from AppResources.
            appBarPlacesButton = new ApplicationBarIconButton(new Uri("/Assets/appbar.show.route.png", UriKind.Relative));
            appBarPlacesButton.Text = "places";
            appBarPlacesButton.Click += new EventHandler(Places_Click);
            appBarPlacesButton.IsEnabled = false;
            ApplicationBar.Buttons.Add(appBarPlacesButton);

            appBarLocateMeButton = new ApplicationBarIconButton(new Uri("/Assets/appbar.locate.me.png", UriKind.Relative));
            appBarLocateMeButton.Text = "My location";
            appBarLocateMeButton.Click += new EventHandler(LocateMe_Click);
            appBarLocateMeButton.IsEnabled = false;
            ApplicationBar.Buttons.Add(appBarLocateMeButton);

            shareIconButton = new ApplicationBarIconButton();
            shareIconButton.IconUri = new Uri("/View/images/icon_tick.png", UriKind.Relative);
            shareIconButton.Text = AppResources.ShareLocation_Txt;
            shareIconButton.Click += new EventHandler(shareBtn_Click);
            shareIconButton.IsEnabled = false;
            ApplicationBar.Buttons.Add(shareIconButton);
        }

        private void Places_Click(object sender, EventArgs e)
        {
            if (PlacesGrid.Visibility == Visibility.Collapsed)
                GetPlaces();
            else
                PlacesGrid.Visibility = Visibility.Collapsed;
        }

        void GetPlaces()
        {
            ShowProgressIndicator("Getting Places");
            string url = string.Format("{0}?at={1},{2}&app_id={3}&app_code={4}&tf=plain&pretty=true", nokiaPlacesUrl, SelectedCoordinate.Latitude, SelectedCoordinate.Longitude, nokiaPlacesAppID, nokiaPlacesAppCode);
            AccountUtils.createNokiaPlacesGetRequest(url, new AccountUtils.postResponseFunction(PlacesResult_Callback));
        }

        void PlacesResult_Callback(JObject obj)
        {
            Places = this.ParsePlaces(obj.ToString());
            Deployment.Current.Dispatcher.BeginInvoke(new Action(delegate
                {
                    PlacesGrid.Visibility = Visibility.Visible;
                    DrawMapMarkers();
                    DrawMapMarkers();
                    Places.Insert(0, new Place() { position = SelectedCoordinate, title = "My Location" });
                    SelectedPlace = Places[0];
                    MyMap.SetView(SelectedPlace.position, MyMap.ZoomLevel, MapAnimationKind.Parabolic);
                    PlacesList.ItemsSource = Places;
                    HideProgressIndicator();
                }));
        }

        private List<Place> ParsePlaces(string json)
        {
            List<Place> places = new List<Place>();
            if (json == string.Empty)
                return places;

            JToken jToken = JObject.Parse(json)["results"]["items"];
            if (jToken == null)
                return places;

            var jlList = jToken.Children();
            foreach (JToken token in jlList)
            {
                Place place = JsonConvert.DeserializeObject<Place>(token.ToString());

                if (place.type == "urn:nlp-types:place")
                {
                    var jTokenListPosition = token["position"];
                    GeoCoordinate position = new GeoCoordinate();
                    position.Latitude = double.Parse(jTokenListPosition.First.ToString());
                    position.Longitude = double.Parse(jTokenListPosition.Last.ToString());
                    place.position = position;
                    places.Add(place);
                }
            }

            return places;
        }

        private void LocateMe_Click(object sender, EventArgs e)
        {
            if (_isLocationAllowed)
            {
                GetCurrentCoordinate();
            }
            else
            {
                MessageBoxResult result = MessageBox.Show("NO LOCATION ALLOWED",
                                                          "",
                                                          MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.OK)
                {
                    _isLocationAllowed = true;
                    //SaveSettings();
                    GetCurrentCoordinate();
                }
            }
        }

        private void ZoomLevelChanged(object sender, EventArgs e)
        {
            DrawMapMarkers();
        }

        private void DrawMapMarkers()
        {
            MyMap.Layers.Clear();
            MapLayer mapLayer = new MapLayer();

            if (SelectedCoordinate != null)
            {
                DrawAccuracyRadius(mapLayer);
                DrawMapMarker(SelectedCoordinate, Colors.Orange, mapLayer);
            }

            MyMap.Layers.Add(mapLayer);
        }

        private void DrawMapMarker(GeoCoordinate coordinate, Color color, MapLayer mapLayer)
        {
            // Create a map marker
            Polygon polygon = new Polygon();
            polygon.Points.Add(new Point(0, 0));
            polygon.Points.Add(new Point(0, 75));
            polygon.Points.Add(new Point(25, 0));
            polygon.Fill = new SolidColorBrush(color);

            // Enable marker to be tapped for location information
            polygon.Tag = new GeoCoordinate(coordinate.Latitude, coordinate.Longitude);

            // Create a MapOverlay and add marker.
            MapOverlay overlay = new MapOverlay();
            overlay.Content = polygon;
            overlay.GeoCoordinate = new GeoCoordinate(coordinate.Latitude, coordinate.Longitude);
            overlay.PositionOrigin = new Point(0.0, 1.0);
            mapLayer.Add(overlay);
        }

        private void DrawAccuracyRadius(MapLayer mapLayer)
        {
            // The ground resolution (in meters per pixel) varies depending on the level of detail 
            // and the latitude at which it’s measured. It can be calculated as follows:
            double metersPerPixels = (Math.Cos(SelectedCoordinate.Latitude * Math.PI / 180) * 2 * Math.PI * 6378137) / (256 * Math.Pow(2, MyMap.ZoomLevel));
            double radius = _accuracy / metersPerPixels;

            Ellipse ellipse = new Ellipse();
            ellipse.Width = radius * 2;
            ellipse.Height = radius * 2;
            ellipse.Fill = new SolidColorBrush(Color.FromArgb(75, 200, 0, 0));

            MapOverlay overlay = new MapOverlay();
            overlay.Content = ellipse;
            overlay.GeoCoordinate = new GeoCoordinate(SelectedCoordinate.Latitude, SelectedCoordinate.Longitude);
            overlay.PositionOrigin = new Point(0.5, 0.5);
            mapLayer.Add(overlay);
        }

        private async void GetCurrentCoordinate()
        {
            ShowProgressIndicator("Getting Location");
            Geolocator geolocator = new Geolocator();
            geolocator.DesiredAccuracy = PositionAccuracy.High;

            try
            {
                Geoposition currentPosition = await geolocator.GetGeopositionAsync(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10));
                _accuracy = currentPosition.Coordinate.Accuracy;

                Dispatcher.BeginInvoke(() =>
                {
                    MyCoordinate = new GeoCoordinate(currentPosition.Coordinate.Latitude, currentPosition.Coordinate.Longitude);
                    SelectedCoordinate = MyCoordinate;
                    SelectedPlace = new Place() { position = MyCoordinate, title = "My Location" };
                    DrawMapMarkers();
                    MyMap.SetView(MyCoordinate, 16, MapAnimationKind.Parabolic);
                    appBarLocateMeButton.IsEnabled = true;
                    appBarPlacesButton.IsEnabled = true;
                    shareIconButton.IsEnabled = true;
                    GetPlaces();
                });
            }
            catch (Exception ex)
            {
                // Couldn't get current location - location might be disabled in settings
                MessageBox.Show("Location might be disabled", "", MessageBoxButton.OK);
            }

            HideProgressIndicator();
        }

        private void ShowProgressIndicator(String msg)
        {
            if (ProgressIndicator == null)
            {
                ProgressIndicator = new ProgressIndicator();
                ProgressIndicator.IsIndeterminate = true;
            }

            ProgressIndicator.Text = msg;
            ProgressIndicator.IsVisible = true;
            SystemTray.SetProgressIndicator(this, ProgressIndicator);
        }

        private void HideProgressIndicator()
        {
            ProgressIndicator.IsVisible = false;
            SystemTray.SetProgressIndicator(this, ProgressIndicator);
        }

        public ShareLocation()
        {
            InitializeComponent();
            GetCurrentCoordinate();
            BuildApplicationBar();
        }

        private void shareBtn_Click(object sender, EventArgs e)
        {
            byte[] thumbnailBytes = captureThumbnail();
            JObject metadata = new JObject();
            JArray filesData = new JArray();
            JObject singleFileInfo = new JObject();
            singleFileInfo[HikeConstants.FILE_NAME] = SelectedPlace == null ? "Location" : SelectedPlace.title;
            singleFileInfo[HikeConstants.FILE_CONTENT_TYPE] = "hikemap/location";
            singleFileInfo[HikeConstants.LATITUDE] = SelectedCoordinate.Latitude;
            singleFileInfo[HikeConstants.LONGITUDE] = SelectedCoordinate.Longitude;
            singleFileInfo[HikeConstants.ZOOM_LEVEL] = MyMap.ZoomLevel;
            singleFileInfo[HikeConstants.LOCATION_ADDRESS] = SelectedPlace== null || SelectedPlace.vicinity == null ? "" : SelectedPlace.vicinity;

            filesData.Add(singleFileInfo.ToObject<JToken>());

            metadata[HikeConstants.FILES_DATA] = filesData;

            object[] locationDetails = new object[2];
            locationDetails[0] = metadata;
            locationDetails[1] = thumbnailBytes;
            PhoneApplicationService.Current.State[HikeConstants.SHARED_LOCATION] = locationDetails;
            MyMap = null;
            NavigationService.GoBack();
        }

        private void map_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Point p = e.GetPosition(this.MyMap);
            GeoCoordinate geo = new GeoCoordinate();
            geo = MyMap.ConvertViewportPointToGeoCoordinate(p);
            MyMap.SetView(geo, MyMap.ZoomLevel, MapAnimationKind.Parabolic);
            SelectedCoordinate = geo;

            if (String.IsNullOrEmpty(_searchString))
                GetPlaces();
            else
                Search();

            DrawMapMarkers();
        }

        private byte[] captureThumbnail()
        {
            byte[] thumbnailBytes = null;
            WriteableBitmap screenshot = new WriteableBitmap(MyMap, new TranslateTransform());

            using (MemoryStream ms = new MemoryStream())
            {
                screenshot.SaveJpeg(ms, HikeConstants.LOCATION_THUMBNAIL_MAX_WIDTH, HikeConstants.LOCATION_THUMBNAIL_MAX_HEIGHT, 0, 100);

                thumbnailBytes = ms.ToArray();
            }

            return thumbnailBytes;
        }

        private void PlacesList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var place = (sender as LongListSelector).SelectedItem as Place;

            if (place != null)
            {
                this.DataContext = place;
                SelectedPlace = place;
                SelectedCoordinate = place.position;
                MyMap.SetView(SelectedCoordinate, MyMap.ZoomLevel, MapAnimationKind.Parabolic);
                DrawMapMarkers();
            }
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (PlacesGrid.Visibility == Visibility.Visible)
            {
                PlacesGrid.Visibility = Visibility.Collapsed;
                e.Cancel = true;
            }

            base.OnBackKeyPress(e);
        }

        String _searchString = "";

        private void PhoneTextBox_ActionIconTapped_1(object sender, EventArgs e)
        {
            var searchString = SearchTextBox.Text.Trim();
            
            if (_searchString.Equals(searchString))
                return;

            _searchString = searchString;
            Search();
        }

        void Search()
        {
            ShowProgressIndicator("Searching for " + _searchString);
            string url = string.Format("{0}?q={1}&at={2},{3}&app_id={4}&app_code={5}&tf=plain&pretty=true", nokiaSeacrhUrl,_searchString, SelectedCoordinate.Latitude, SelectedCoordinate.Longitude, nokiaPlacesAppID, nokiaPlacesAppCode);
            AccountUtils.createNokiaPlacesGetRequest(url, new AccountUtils.postResponseFunction(PlacesResult_Callback));
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Place
    {
        [JsonProperty]
        public int distance { get; set; }

        [JsonProperty]
        public string title { get; set; }

        [JsonProperty]
        public double averageRating { get; set; }

        [JsonProperty]
        public string icon { get; set; }

        [JsonProperty]
        public string vicinity { get; set; }

        [JsonProperty]
        public string type { get; set; }

        [JsonProperty]
        public string href { get; set; }

        [JsonProperty]
        public string id { get; set; }

        public GeoCoordinate position { get; set; }

        [JsonProperty]
        public Category category { get; set; }

    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Category
    {
        [JsonProperty]
        public string id { get; set; }

        [JsonProperty]
        public string title { get; set; }

        [JsonProperty]
        public string href { get; set; }

        [JsonProperty]
        public string type { get; set; }
    }
}