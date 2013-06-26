using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Phone.Controls;
using System.Windows.Media.Imaging;
using System.IO;
using System.Device.Location;
using System.Windows.Input;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Shell;
using Newtonsoft.Json.Linq;
using windows_client.Languages;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Services;
using Windows.Devices.Geolocation;
using System.Collections.Generic;
using System.Windows.Shapes;
using Newtonsoft.Json;
using System.Linq;
using windows_client.utils;

namespace windows_client.View
{
    public partial class ShareLocation : PhoneApplicationPage
    {
        ApplicationBarIconButton shareIconButton = null;
        ApplicationBarIconButton appBarLocateMeButton = null;
        ApplicationBarIconButton appBarPlacesButton = null;
        
        // Application bar menu items
        private ApplicationBarMenuItem AppBarColorModeMenuItem = null;
        private ApplicationBarMenuItem AppBarLandmarksMenuItem = null;
        private ApplicationBarMenuItem AppBarPedestrianFeaturesMenuItem = null;

        // Progress indicator shown in system tray
        private ProgressIndicator ProgressIndicator = null;

        // My current location
        private GeoCoordinate MyCoordinate = null;
        private GeoCoordinate SelectedCoordinate = null;

        // List of coordinates representing search hits / destination of route
        private List<GeoCoordinate> MyCoordinates = new List<GeoCoordinate>();

        // Reverse geocode query
        private ReverseGeocodeQuery MyReverseGeocodeQuery = null;

        /// <summary>
        /// True when this object instance has been just created, otherwise false
        /// </summary>
        private bool _isNewInstance = true;

        /// <summary>
        /// True when access to user location is allowed, otherwise false
        /// </summary>
        private bool _isLocationAllowed = false;

        /// <summary>
        /// True when color mode has been temporarily set to light, otherwise false
        /// </summary>
        private bool _isTemporarilyLight = false;

        /// <summary>
        /// True when route is being searched, otherwise false
        /// </summary>
        private bool _isPlacesSearch = false;

        /// <summary>
        /// Accuracy of my current location in meters;
        /// </summary>
        private double _accuracy = 0.0;

        private string nokiaPlacesUrl = "http://demo.places.nlp.nokia.com/places/v1/discover/explore";
        private string nokiaPlaceUrl = "http://demo.places.nlp.nokia.com/places/v1/places/";
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

            // Create new menu items with the localized strings from AppResources.
            AppBarColorModeMenuItem = new ApplicationBarMenuItem("ColorModeDarkMenuItemText");
            AppBarColorModeMenuItem.Click += new EventHandler(ColorMode_Click);
            ApplicationBar.MenuItems.Add(AppBarColorModeMenuItem);

            AppBarLandmarksMenuItem = new ApplicationBarMenuItem("LandmarksOnMenuItemText");
            AppBarLandmarksMenuItem.Click += new EventHandler(Landmarks_Click);
            ApplicationBar.MenuItems.Add(AppBarLandmarksMenuItem);

            AppBarPedestrianFeaturesMenuItem = new ApplicationBarMenuItem("PedestrianFeaturesOnMenuItemText");
            AppBarPedestrianFeaturesMenuItem.Click += new EventHandler(PedestrianFeatures_Click);
            ApplicationBar.MenuItems.Add(AppBarPedestrianFeaturesMenuItem);
        }

        private void Places_Click(object sender, EventArgs e)
        {
            if (PlacesList.Visibility == Visibility.Collapsed)
            {
                string url = string.Format("{0}?at={1},{2}&app_id={3}&app_code={4}&tf=plain&pretty=true", nokiaPlacesUrl, SelectedCoordinate.Latitude, SelectedCoordinate.Longitude, nokiaPlacesAppID, nokiaPlacesAppCode);
                AccountUtils.createGetRequest(url, new AccountUtils.postResponseFunction(PlacesResult_Callback), false);
                PlacesList.Visibility = Visibility.Visible;
                stackPanelDetail.Visibility = Visibility.Collapsed;
                ModePanel.Visibility = Visibility.Collapsed;
                PitchSlider.Visibility = Visibility.Collapsed;
                HeadingSlider.Visibility = Visibility.Collapsed;
            }
            else
            {
                PlacesList.Visibility = Visibility.Collapsed;
                ModePanel.Visibility = Visibility.Visible;
                PitchSlider.Visibility = Visibility.Visible;
                HeadingSlider.Visibility = Visibility.Visible;
            }
        }

        public void PlacesResult_Callback(JObject obj)
        {
            Places = this.ParsePlaces(obj.ToString());
            Deployment.Current.Dispatcher.BeginInvoke(new Action(delegate
                {
                    DrawMapMarkers();
                    PlacesList.ItemsSource = Places;
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
                    MyCoordinates.Add(position);
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

        private void ColorMode_Click(object sender, EventArgs e)
        {
            if (MyMap.ColorMode == MapColorMode.Dark)
            {
                MyMap.ColorMode = MapColorMode.Light;
                AppBarColorModeMenuItem.Text = "Drk";
            }
            else
            {
                MyMap.ColorMode = MapColorMode.Dark;
                AppBarColorModeMenuItem.Text = "light";
            }
        }

        private void Landmarks_Click(object sender, EventArgs e)
        {
            MyMap.LandmarksEnabled = !MyMap.LandmarksEnabled;
            if (MyMap.LandmarksEnabled)
            {
                AppBarLandmarksMenuItem.Text = "Landmarks off";
            }
            else
            {
                AppBarLandmarksMenuItem.Text = "Landmarks on";
            }
        }

        private void PedestrianFeatures_Click(object sender, EventArgs e)
        {
            MyMap.PedestrianFeaturesEnabled = !MyMap.PedestrianFeaturesEnabled;
            if (MyMap.PedestrianFeaturesEnabled)
            {
                AppBarPedestrianFeaturesMenuItem.Text = "pedes off";
            }
            else
            {
                AppBarPedestrianFeaturesMenuItem.Text = "pedes on";
            }
        }

        private void PitchValueChanged(object sender, EventArgs e)
        {
            if (PitchSlider != null)
            {
                MyMap.Pitch = PitchSlider.Value;
            }
        }

        private void HeadingValueChanged(object sender, EventArgs e)
        {
            if (HeadingSlider != null)
            {
                double value = HeadingSlider.Value;
                if (value > 360) value -= 360;
                MyMap.Heading = value;
            }
        }

        private void CartographicModeButton_Click(object sender, EventArgs e)
        {
            RoadButton.IsEnabled = true;
            AerialButton.IsEnabled = true;
            HybridButton.IsEnabled = true;
            TerrainButton.IsEnabled = true;
            AppBarColorModeMenuItem.IsEnabled = false;

            if (sender == RoadButton)
            {
                AppBarColorModeMenuItem.IsEnabled = true;
                // To change color mode back to dark
                if (_isTemporarilyLight)
                {
                    _isTemporarilyLight = false;
                    MyMap.ColorMode = MapColorMode.Dark;
                }
                MyMap.CartographicMode = MapCartographicMode.Road;
                RoadButton.IsEnabled = false;
            }
            else if (sender == AerialButton)
            {
                MyMap.CartographicMode = MapCartographicMode.Aerial;
                AerialButton.IsEnabled = false;
            }
            else if (sender == HybridButton)
            {
                MyMap.CartographicMode = MapCartographicMode.Hybrid;
                HybridButton.IsEnabled = false;
            }
            else if (sender == TerrainButton)
            {
                // To enable terrain mode when color mode is dark
                if (MyMap.ColorMode == MapColorMode.Dark)
                {
                    _isTemporarilyLight = true;
                    MyMap.ColorMode = MapColorMode.Light;
                }
                MyMap.CartographicMode = MapCartographicMode.Terrain;
                TerrainButton.IsEnabled = false;
            }
        }

        private void ZoomLevelChanged(object sender, EventArgs e)
        {
            DrawMapMarkers();
        }

        private void Marker_Click(object sender, EventArgs e)
        {
            Polygon p = (Polygon)sender;
            GeoCoordinate geoCoordinate = (GeoCoordinate)p.Tag;
            Place place = Places.Where(pl => pl.position == geoCoordinate).FirstOrDefault();

            if (place != null)
            {
                this.DataContext = place;
                SelectedPlace = place;
                SelectedCoordinate = place.position;
                PlacesList.Visibility = Visibility.Collapsed;
                stackPanelDetail.Visibility = Visibility.Visible;
                image.Source = null;
                string url = string.Format("{0}{1}?app_id={2}&app_code={3}", nokiaPlaceUrl, place.id, nokiaPlacesAppID, nokiaPlacesAppCode);
                AccountUtils.createGetRequest(url, new AccountUtils.postResponseFunction(PlaceResult_Callback), false);
                DrawMapMarkers();
            }
        }

        public void PlaceResult_Callback(JObject obj)
        {
            Deployment.Current.Dispatcher.BeginInvoke(new Action(delegate
                {
                    try
                    {
                        string firstImageUrl = JObject.Parse(obj.ToString())["media"]["images"]["items"].First["src"].ToString();
                        image.Source = new BitmapImage(new Uri(firstImageUrl, UriKind.Absolute));
                    }
                    catch { }
                }));
        }

        private void DrawMapMarkers()
        {
            MyMap.Layers.Clear();
            MapLayer mapLayer = new MapLayer();

            // Draw marker for current position
            if (MyCoordinate != null)
            {
                DrawAccuracyRadius(mapLayer);
                DrawMapMarker(MyCoordinate, Colors.Green, mapLayer);
            }

            if (SelectedCoordinate != null && SelectedCoordinate != MyCoordinate)
            {
                DrawAccuracyRadius(mapLayer);
                DrawMapMarker(SelectedCoordinate, Colors.Red, mapLayer);
            }

            // Draw markers for location(s) / destination(s)
            for (int i = 0; i < MyCoordinates.Count; i++)
            {
                if (MyCoordinates[i] != SelectedCoordinate && MyCoordinates[i] != MyCoordinate)
                    DrawMapMarker(MyCoordinates[i], Colors.Blue, mapLayer);
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
            polygon.MouseLeftButtonUp += new MouseButtonEventHandler(Marker_Click);

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
            double metersPerPixels = (Math.Cos(MyCoordinate.Latitude * Math.PI / 180) * 2 * Math.PI * 6378137) / (256 * Math.Pow(2, MyMap.ZoomLevel));
            double radius = _accuracy / metersPerPixels;

            Ellipse ellipse = new Ellipse();
            ellipse.Width = radius * 2;
            ellipse.Height = radius * 2;
            ellipse.Fill = new SolidColorBrush(Color.FromArgb(75, 200, 0, 0));

            MapOverlay overlay = new MapOverlay();
            overlay.Content = ellipse;
            overlay.GeoCoordinate = new GeoCoordinate(MyCoordinate.Latitude, MyCoordinate.Longitude);
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
                    DrawMapMarkers();
                    MyMap.SetView(MyCoordinate, 10, MapAnimationKind.Parabolic);
                    appBarLocateMeButton.IsEnabled = true;
                    appBarPlacesButton.IsEnabled = true;
                    shareIconButton.IsEnabled = true;
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

        private void ReverseGeocodeQuery_QueryCompleted(object sender, QueryCompletedEventArgs<IList<MapLocation>> e)
        {
            String msgBoxText = "";
            if (e.Error == null)
            {
                if (e.Result.Count > 0)
                {
                    MapAddress address = e.Result[0].Information.Address;
                    if (address.Street.Length > 0)
                    {
                        msgBoxText += "\n" + address.Street;
                        if (address.HouseNumber.Length > 0) msgBoxText += " " + address.HouseNumber;
                    }
                    if (address.PostalCode.Length > 0)
                        msgBoxText += "\n" + address.PostalCode;
                    if (address.City.Length > 0)
                        msgBoxText += "\n" + address.City;
                    if (address.Country.Length > 0)
                        msgBoxText += "\n" + address.Country;
                }

                MyReverseGeocodeQuery.Dispose();
            }

            byte[] thumbnailBytes = captureThumbnail();
            JObject metadata = new JObject();
            JArray filesData = new JArray();
            JObject singleFileInfo = new JObject();
            singleFileInfo[HikeConstants.FILE_NAME] = (e.Error == null && e.Result.Count > 0) ? e.Result[0].Information.Name + "\n" + e.Result[0].Information.Description : "Location";
            singleFileInfo[HikeConstants.FILE_CONTENT_TYPE] = "hikemap/location";
            singleFileInfo[HikeConstants.LATITUDE] = SelectedCoordinate.Latitude;
            singleFileInfo[HikeConstants.LONGITUDE] = SelectedCoordinate.Longitude;
            singleFileInfo[HikeConstants.ZOOM_LEVEL] = MyMap.ZoomLevel;
            singleFileInfo[HikeConstants.LOCATION_ADDRESS] = (e.Error == null && e.Result.Count > 0) ? msgBoxText : "";

            filesData.Add(singleFileInfo.ToObject<JToken>());

            metadata[HikeConstants.FILES_DATA] = filesData;

            object[] locationDetails = new object[2];
            locationDetails[0] = metadata;
            locationDetails[1] = thumbnailBytes;
            PhoneApplicationService.Current.State[HikeConstants.SHARED_LOCATION] = locationDetails;
            MyMap = null;
            NavigationService.GoBack();
        }

        private void shareBtn_Click(object sender, EventArgs e)
        {
            MyCoordinates.Clear();
            DrawMapMarkers();

            if (SelectedPlace == null)
            {
                if (MyReverseGeocodeQuery == null || !MyReverseGeocodeQuery.IsBusy)
                {
                    MyReverseGeocodeQuery = new ReverseGeocodeQuery();
                    MyReverseGeocodeQuery.GeoCoordinate = new GeoCoordinate(SelectedCoordinate.Latitude, SelectedCoordinate.Longitude);
                    MyReverseGeocodeQuery.QueryCompleted += ReverseGeocodeQuery_QueryCompleted;
                    MyReverseGeocodeQuery.QueryAsync();
                }
            }
            else
            {
                byte[] thumbnailBytes = captureThumbnail();
                JObject metadata = new JObject();
                JArray filesData = new JArray();
                JObject singleFileInfo = new JObject();
                singleFileInfo[HikeConstants.FILE_NAME] = SelectedPlace.title;
                singleFileInfo[HikeConstants.FILE_CONTENT_TYPE] = "hikemap/location";
                singleFileInfo[HikeConstants.LATITUDE] = SelectedCoordinate.Latitude;
                singleFileInfo[HikeConstants.LONGITUDE] = SelectedCoordinate.Longitude;
                singleFileInfo[HikeConstants.ZOOM_LEVEL] = MyMap.ZoomLevel;
                singleFileInfo[HikeConstants.LOCATION_ADDRESS] = SelectedPlace.vicinity;

                filesData.Add(singleFileInfo.ToObject<JToken>());

                metadata[HikeConstants.FILES_DATA] = filesData;

                object[] locationDetails = new object[2];
                locationDetails[0] = metadata;
                locationDetails[1] = thumbnailBytes;
                PhoneApplicationService.Current.State[HikeConstants.SHARED_LOCATION] = locationDetails;
                MyMap = null;
                NavigationService.GoBack();
            }
        }

        private void map_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Point p = e.GetPosition(this.MyMap);
            GeoCoordinate geo = new GeoCoordinate();
            geo = MyMap.ConvertViewportPointToGeoCoordinate(p);
            MyMap.Center = geo;
            SelectedCoordinate = geo;
            SelectedPlace = null;
            DrawMapMarkers();
        }

        private byte[] captureThumbnail()
        {
            byte[] thumbnailBytes = null;
            WriteableBitmap screenshot = new WriteableBitmap(MyMap, new TranslateTransform());
            using (MemoryStream ms = new MemoryStream())
            {
                screenshot.SaveJpeg(ms, HikeConstants.LOCATION_THUMBNAIL_MAX_WIDTH, HikeConstants.LOCATION_THUMBNAIL_MAX_HEIGHT,
                    0, 47);
                thumbnailBytes = ms.ToArray();
            }
            return thumbnailBytes;
        }

        private void PlacesList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var place = (sender as LongListSelector).SelectedItem as Place;

            if (place != null)
            {
                PlacesList.Visibility = Visibility.Collapsed;
                this.DataContext = place;
                SelectedPlace = place;
                SelectedCoordinate = place.position;
                stackPanelDetail.Visibility = Visibility.Visible;
                image.Source = null;
                string url = string.Format("{0}{1}?app_id={2}&app_code={3}", nokiaPlaceUrl, place.id, nokiaPlacesAppID, nokiaPlacesAppCode);
                AccountUtils.createGetRequest(url, new AccountUtils.postResponseFunction(PlaceResult_Callback), false);
                DrawMapMarkers();
                (sender as LongListSelector).SelectedItem = null;
            }
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if(stackPanelDetail.Visibility == Visibility.Visible)
            {
                stackPanelDetail.Visibility = Visibility.Collapsed;
                PlacesList.Visibility = Visibility.Visible;
                e.Cancel = true;
            }
            else if (PlacesList.Visibility == Visibility.Visible)
            {
                PlacesList.Visibility = Visibility.Collapsed;
                e.Cancel = true;
            }

            base.OnBackKeyPress(e);
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