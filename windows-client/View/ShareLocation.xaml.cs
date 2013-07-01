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
using System.ComponentModel;
using System.Windows.Controls;

namespace windows_client.View
{
    public partial class ShareLocation : PhoneApplicationPage
    {
        ApplicationBarIconButton shareIconButton = null;
        
        // Progress indicator shown in system tray
        private ProgressIndicator _progressIndicator = null;

        // My current location
        private GeoCoordinate _myCoordinate = null;
        private String _myPlaceVicinity= null;
        private GeoCoordinate _selectedCoordinate = null;

        /// <summary>
        /// True when route is being searched, otherwise false
        /// </summary>
        private bool _isPlacesSearch = false;

        /// <summary>
        /// Accuracy of my current location in meters;
        /// </summary>
        private double _accuracy = 0.0;

        private string _nokiaPlacesUrl = "http://places.nlp.nokia.com/places/v1/discover/explore";
        private string _nokiaSeacrhUrl = "http://places.nlp.nokia.com/places/v1/discover/search";
        private string _nokiaHereUrl = "http://places.nlp.nokia.com/places/v1/discover/here";
        private string _nokiaPlacesAppID = "KyG8YRrgO09I7bm9jXwd";
        private string _nokiaPlacesAppCode = "Uhjk-Gny7_A-ISaJb3DMKQ";
        private List<Place> _places;
        Place _selectedPlace;

        private void BuildApplicationBar()
        {
            // Set the page's ApplicationBar to a new instance of ApplicationBar.    
            ApplicationBar = new ApplicationBar();

            ApplicationBar.Mode = ApplicationBarMode.Default;
            ApplicationBar.IsVisible = true;
            ApplicationBar.Opacity = 1.0;
            ApplicationBar.IsMenuEnabled = true;

            shareIconButton = new ApplicationBarIconButton();
            shareIconButton.IconUri = new Uri("/View/images/icon_tick.png", UriKind.Relative);
            shareIconButton.Text = AppResources.ShareLocation_Txt;
            shareIconButton.Click += new EventHandler(shareBtn_Click);
            shareIconButton.IsEnabled = false;
            ApplicationBar.Buttons.Add(shareIconButton);
        }

        void GetMyPlaceDetails()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    ShowProgressIndicator();
                });

            string url = string.Format("{0}?at={1},{2}&app_id={3}&app_code={4}&tf=plain&pretty=true", _nokiaHereUrl, _selectedCoordinate.Latitude, _selectedCoordinate.Longitude, _nokiaPlacesAppID, _nokiaPlacesAppCode);
            AccountUtils.createNokiaPlacesGetRequest(url, new AccountUtils.postResponseFunction(MyPlace_Callback));
        }

        void MyPlace_Callback(JObject obj)
        {
            if (obj == null)
                return;

            try
            {
                JToken jToken = obj["search"]["context"]["location"]["address"];
                _myPlaceVicinity = jToken["text"].ToString().Replace("\n", ", ");
            }
            catch
            {
                _myPlaceVicinity = String.Empty;
            }

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                HideProgressIndicator();
            });
        }

        void GetPlaces()
        {
            ShowProgressIndicator();
            string url = string.Format("{0}?at={1},{2}&app_id={3}&app_code={4}&tf=plain&pretty=true", _nokiaPlacesUrl, _selectedCoordinate.Latitude, _selectedCoordinate.Longitude, _nokiaPlacesAppID, _nokiaPlacesAppCode);
            AccountUtils.createNokiaPlacesGetRequest(url, new AccountUtils.postResponseFunction(PlacesResult_Callback));
        }

        void PlacesResult_Callback(JObject obj)
        {
            _isPlacesSearch = true;
            _places = this.ParsePlaces(obj);
            
            Deployment.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                PlacesGrid.Visibility = Visibility.Visible;
                DrawMapMarkers();
                DrawMapMarkers();
                _selectedPlace = new Place() { position = _selectedCoordinate, title = "My Location", vicinity = _myPlaceVicinity };
                _places.Insert(0, _selectedPlace);
                _selectedPlace = _places[0];
                MyMap.SetView(_selectedPlace.position, MyMap.ZoomLevel, MapAnimationKind.Parabolic);
                PlacesList.ItemsSource = _places;
                HideProgressIndicator();
                PlacesList.SelectedItem = _places[0];
            }));
        }

        private List<Place> ParsePlaces(JObject json)
        {
            List<Place> places = new List<Place>();
            if (json == null)
                return places;

            JToken jToken = json["results"]["items"];
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

        private void ZoomLevelChanged(object sender, EventArgs e)
        {
            DrawMapMarkers();
        }

        private void DrawMapMarkers()
        {
            if (MyMap != null)
            {
                MyMap.Layers.Clear();
                MapLayer mapLayer = new MapLayer();

                if (_selectedCoordinate != null)
                {
                    DrawAccuracyRadius(mapLayer);
                    DrawMapMarker(_selectedCoordinate, Colors.Orange, mapLayer);
                }

                MyMap.Layers.Add(mapLayer);
            }
        }

        private void DrawMapMarker(GeoCoordinate coordinate, Color color, MapLayer mapLayer)
        {
            // Create a map marker
            Polygon polygon = new Polygon();
            polygon.Points.Add(new Point(0, 0));
            polygon.Points.Add(new Point(0, 55));
            polygon.Points.Add(new Point(25, 25));
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
            double metersPerPixels = (Math.Cos(_selectedCoordinate.Latitude * Math.PI / 180) * 2 * Math.PI * 6378137) / (256 * Math.Pow(2, MyMap.ZoomLevel));
            double radius = _accuracy / metersPerPixels;

            Ellipse ellipse = new Ellipse();
            ellipse.Width = radius * 2;
            ellipse.Height = radius * 2;
            ellipse.Fill = new SolidColorBrush(Color.FromArgb(20, 0, 0, 0));
            ellipse.Stroke = new SolidColorBrush(Color.FromArgb(75, 0, 0, 0));

            MapOverlay overlay = new MapOverlay();
            overlay.Content = ellipse;
            overlay.GeoCoordinate = new GeoCoordinate(_selectedCoordinate.Latitude, _selectedCoordinate.Longitude);
            overlay.PositionOrigin = new Point(0.5, 0.5);
            mapLayer.Add(overlay);
        }

        private async void GetCurrentCoordinate()
        {
            ShowProgressIndicator();
            Geolocator geolocator = new Geolocator();
            geolocator.DesiredAccuracy = PositionAccuracy.High;

            try
            {
                Geoposition currentPosition = await geolocator.GetGeopositionAsync(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10));
                _accuracy = currentPosition.Coordinate.Accuracy;
                _myCoordinate = new GeoCoordinate(currentPosition.Coordinate.Latitude, currentPosition.Coordinate.Longitude);
                _selectedCoordinate = _myCoordinate;

                Dispatcher.BeginInvoke(() =>
                {
                    DrawMapMarkers();
                    MyMap.SetView(_myCoordinate, 16, MapAnimationKind.Parabolic);
                });

                GetMyPlaceDetails();
                GetPlaces();
            }
            catch (Exception ex)
            {
                // Couldn't get current location - location might be disabled in settings
                MessageBox.Show("Location might be disabled", "", MessageBoxButton.OK);
            }

            HideProgressIndicator();
        }

        private void ShowProgressIndicator()
        {
            if (_progressIndicator == null)
            {
                _progressIndicator = new ProgressIndicator();
                _progressIndicator.IsIndeterminate = true;
            }

            _progressIndicator.IsVisible = true;
            SystemTray.SetProgressIndicator(this, _progressIndicator);
        }

        private void HideProgressIndicator()
        {
            _progressIndicator.IsVisible = false;
            SystemTray.SetProgressIndicator(this, _progressIndicator);
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
            singleFileInfo[HikeConstants.FILE_NAME] = _selectedPlace == null ? "Location" : _selectedPlace.title;
            singleFileInfo[HikeConstants.FILE_CONTENT_TYPE] = "hikemap/location";
            singleFileInfo[HikeConstants.LATITUDE] = _selectedCoordinate.Latitude;
            singleFileInfo[HikeConstants.LONGITUDE] = _selectedCoordinate.Longitude;
            singleFileInfo[HikeConstants.ZOOM_LEVEL] = MyMap.ZoomLevel;
            singleFileInfo[HikeConstants.LOCATION_ADDRESS] = _selectedPlace== null || _selectedPlace.vicinity == null ? "" : _selectedPlace.vicinity;

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
            _selectedCoordinate = geo;

            shareIconButton.IsEnabled = false;
            _isPlacesSearch = false;

            GetMyPlaceDetails();

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
            var place = (sender as ListBox).SelectedItem as Place;

            if (place != null)
            {
                this.DataContext = place;
                _selectedPlace = place;
                _selectedCoordinate = place.position;
                MyMap.SetView(_selectedCoordinate, MyMap.ZoomLevel, MapAnimationKind.Parabolic);
                DrawMapMarkers();
                shareIconButton.IsEnabled = true;
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

            this.Focus();
            Search();
        }

        void Search()
        {
            ShowProgressIndicator();
            string url = string.Format("{0}?q={1}&at={2},{3}&app_id={4}&app_code={5}&tf=plain&pretty=true", _nokiaSeacrhUrl,_searchString, _selectedCoordinate.Latitude, _selectedCoordinate.Longitude, _nokiaPlacesAppID, _nokiaPlacesAppCode);
            AccountUtils.createNokiaPlacesGetRequest(url, new AccountUtils.postResponseFunction(PlacesResult_Callback));
        }

        private void MyLocation_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            GetCurrentCoordinate();
        }

        private void Places_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (PlacesGrid.Visibility == Visibility.Collapsed)
            {
                if (!_isPlacesSearch)
                    GetPlaces();
                else
                    PlacesGrid.Visibility = Visibility.Visible;
            }
            else
                PlacesGrid.Visibility = Visibility.Collapsed;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Place : INotifyPropertyChanged
    {
        string _vicinity;

        public Visibility VicinityVisibility
        {
            get
            {
                return String.IsNullOrEmpty(vicinity) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        [JsonProperty]
        public int distance { get; set; }

        [JsonProperty]
        public string title { get; set; }

        [JsonProperty]
        public double averageRating { get; set; }

        [JsonProperty]
        public string icon { get; set; }

        [JsonProperty]
        public string vicinity
        {
            get
            {
                return _vicinity;
            }
            set
            {
                _vicinity = value.Replace("\n", ", ");
                NotifyPropertyChanged("vicinity");
                NotifyPropertyChanged("VicinityVisibility");
            }
        }

        [JsonProperty]
        public string type { get; set; }

        [JsonProperty]
        public string href { get; set; }

        [JsonProperty]
        public string id { get; set; }

        public GeoCoordinate position { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify that a property changed
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Places :: NotifyPropertyChanged : NotifyPropertyChanged , Exception : " + ex.StackTrace);
                    }
                });
            }
        }
    }
}