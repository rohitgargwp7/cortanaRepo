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
using Windows.Foundation;

namespace windows_client.View
{
    public partial class ShareLocation : PhoneApplicationPage
    {
        ApplicationBarIconButton shareIconButton = null;
        
        // Progress indicator shown in system tray
        private ProgressIndicator _progressIndicator = null;
        Geolocator _geolocator;

        // My current location
        private GeoCoordinate _myCoordinate = null;
        private String _myPlaceVicinity = String.Empty;
        private GeoCoordinate _selectedCoordinate = null;

        /// <summary>
        /// True when route is being searched, otherwise false
        /// </summary>
        private bool _isPlacesSearch = false;

        private string _nokiaPlacesUrl = "http://places.nlp.nokia.com/places/v1/discover/explore";
        private string _nokiaSeacrhUrl = "http://places.nlp.nokia.com/places/v1/discover/search";
        private string _nokiaHereUrl = "http://places.nlp.nokia.com/places/v1/discover/here";
        private string _nokiaPlacesAppID = "KyG8YRrgO09I7bm9jXwd";
        private string _nokiaPlacesAppCode = "Uhjk-Gny7_A-ISaJb3DMKQ";
        private List<Place> _places;
        Place _selectedPlace;
        Boolean _isMapTapped = false;
        Boolean _isDefaultSelection = false;
        Boolean _isLocationEnabled = true;

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
            if (MyMap == null)
                return;
            
            _isPlacesSearch = true;
            _places = this.ParsePlaces(obj);
            
            Deployment.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                if (MyMap == null)
                    return;
                
                PlacesGrid.Visibility = Visibility.Visible;
                DrawMapMarkers();
                DrawMapMarkers();
                _selectedPlace = new Place() 
                { 
                    position = _selectedCoordinate, 
                    title = _myCoordinate == null || _selectedCoordinate!= _myCoordinate? AppResources.Location_Txt : AppResources.My_Location_Text, 
                    vicinity = _myPlaceVicinity 
                };
                _places.Insert(0, _selectedPlace);
                _selectedPlace = _places[0];
                PlacesList.ItemsSource = _places;
                HideProgressIndicator();
                _isDefaultSelection = true;
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
            polygon.Points.Add(new System.Windows.Point(0, 0));
            polygon.Points.Add(new System.Windows.Point(0, 55));
            polygon.Points.Add(new System.Windows.Point(25, 25));
            polygon.Points.Add(new System.Windows.Point(25, 0));
            polygon.Fill = new SolidColorBrush(color);

            // Enable marker to be tapped for location information
            polygon.Tag = new GeoCoordinate(coordinate.Latitude, coordinate.Longitude);

            // Create a MapOverlay and add marker.
            MapOverlay overlay = new MapOverlay();
            overlay.Content = polygon;
            overlay.GeoCoordinate = new GeoCoordinate(coordinate.Latitude, coordinate.Longitude);
            overlay.PositionOrigin = new System.Windows.Point(0.0, 1.0);
            mapLayer.Add(overlay);
        }

        private void DrawAccuracyRadius(MapLayer mapLayer)
        {
            // The ground resolution (in meters per pixel) varies depending on the level of detail 
            // and the latitude at which it’s measured. It can be calculated as follows:
            double metersPerPixels = (Math.Cos(_selectedCoordinate.Latitude * Math.PI / 180) * 2 * Math.PI * 6378137) / (256 * Math.Pow(2, MyMap.ZoomLevel));
            double radius = 55.0 / metersPerPixels;

            Ellipse ellipse = new Ellipse();
            ellipse.Width = radius * 2;
            ellipse.Height = radius * 2;
            ellipse.Fill = new SolidColorBrush(Color.FromArgb(20, 0, 0, 0));
            ellipse.Stroke = new SolidColorBrush(Color.FromArgb(75, 0, 0, 0));

            MapOverlay overlay = new MapOverlay();
            overlay.Content = ellipse;
            overlay.GeoCoordinate = new GeoCoordinate(_selectedCoordinate.Latitude, _selectedCoordinate.Longitude);
            overlay.PositionOrigin = new System.Windows.Point(0.5, 0.5);
            mapLayer.Add(overlay);
        }

        Boolean _isFetchingCurrentLocation = false;

        private async void GetCurrentCoordinate()
        {
            if (_isFetchingCurrentLocation)
                return;

            if (!_isLocationEnabled)
            {
                _isFetchingCurrentLocation = false;
                HideProgressIndicator();
                return;
            }

            _isFetchingCurrentLocation = true;

            ShowProgressIndicator();

            _geolocator.DesiredAccuracyInMeters = 10;
            _geolocator.MovementThreshold = 5;
            _geolocator.DesiredAccuracy = PositionAccuracy.High;

            IAsyncOperation<Geoposition> locationTask = _geolocator.GetGeopositionAsync(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(3));

            try
            {
                Geoposition currentPosition = await locationTask;

                if (_isMapTapped)
                    return;

                var newCoordinate = new GeoCoordinate(currentPosition.Coordinate.Latitude, currentPosition.Coordinate.Longitude);

                if (_myCoordinate == null || _myCoordinate != newCoordinate)
                {
                    _myCoordinate = newCoordinate;

                    _selectedCoordinate = _myCoordinate;

                    Dispatcher.BeginInvoke(() =>
                    {
                        DrawMapMarkers();
                        MyMap.SetView(_myCoordinate, 16, MapAnimationKind.Parabolic);
                    });

                    GetMyPlaceDetails();

                    if (String.IsNullOrEmpty(_searchString))
                        GetPlaces();
                    else
                        Search();
                }

            }
            catch (Exception ex)
            {
                // Couldn't get current location - location might be disabled in settings
                //MessageBox.Show("Location might be disabled", "", MessageBoxButton.OK);
                System.Diagnostics.Debug.WriteLine("Location exception GetCurrentCoordinate : " + ex.StackTrace);
            }
            finally
            {
                if (locationTask != null)
                {
                    if (locationTask.Status == AsyncStatus.Started)
                        locationTask.Cancel();

                    locationTask.Close();
                }

                _isFetchingCurrentLocation = false;

                HideProgressIndicator();
            }
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
            if (_isFetchingCurrentLocation)
                return;

            _progressIndicator.IsVisible = false;
            SystemTray.SetProgressIndicator(this, _progressIndicator);
        }

        public ShareLocation()
        {
            InitializeComponent();
            BuildApplicationBar(); 
            
            MyMap.Loaded += MyMap_Loaded;
        }

        void MyMap_Loaded(object sender, RoutedEventArgs e)
        {
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.ApplicationId = HikeConstants.MICROSOFT_MAP_SERVICE_APPLICATION_ID;
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.AuthenticationToken = HikeConstants.MICROSOFT_MAP_SERVICE_AUTHENTICATION_TOKEN;
        }

        private void shareBtn_Click(object sender, EventArgs e)
        {
            byte[] thumbnailBytes = captureThumbnail();
            JObject metadata = new JObject();
            JArray filesData = new JArray();
            JObject singleFileInfo = new JObject();
            singleFileInfo[HikeConstants.FILE_NAME] = _selectedPlace == null ? AppResources.Location_Txt : _selectedPlace.title;
            singleFileInfo[HikeConstants.FILE_CONTENT_TYPE] = "hikemap/location";
            singleFileInfo[HikeConstants.LATITUDE] = _selectedCoordinate.Latitude;
            singleFileInfo[HikeConstants.LONGITUDE] = _selectedCoordinate.Longitude;
            singleFileInfo[HikeConstants.ZOOM_LEVEL] = MyMap.ZoomLevel;
            singleFileInfo[HikeConstants.LOCATION_ADDRESS] = _selectedPlace == null || _selectedPlace.vicinity == null ? String.Empty : _selectedPlace.vicinity;

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
            _isMapTapped = true;
            System.Windows.Point p = e.GetPosition(this.MyMap);
            GeoCoordinate geo = new GeoCoordinate();
            geo = MyMap.ConvertViewportPointToGeoCoordinate(p);
            MyMap.SetView(geo, MyMap.ZoomLevel, MapAnimationKind.Parabolic);
            _selectedCoordinate = geo;

            shareIconButton.IsEnabled = true;
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

                if(!_isDefaultSelection)
                    MyMap.SetView(_selectedCoordinate, MyMap.ZoomLevel, MapAnimationKind.Parabolic);

                DrawMapMarkers();
                shareIconButton.IsEnabled = true;
                _isDefaultSelection = false;
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

        private void PhoneTextBox_ActionIconTapped(object sender, EventArgs e)
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
            if (shareIconButton.IsEnabled == false)
                return;

            _isMapTapped = false;
            GetCurrentCoordinate();
        }

        private void Places_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (shareIconButton.IsEnabled == false)
                return;
            
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

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (_geolocator == null)
                _geolocator = new Geolocator();

            if (_geolocator.LocationStatus == PositionStatus.Disabled)
            {
                var result = MessageBox.Show(AppResources.ShareLocation_LocationServiceNotEnabled_Txt, AppResources.Location_Heading, MessageBoxButton.OKCancel);

                if(result == MessageBoxResult.OK)
                    Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-location:"));

                _isLocationEnabled = false;
            }
            else
                _isLocationEnabled = true;

            App.appSettings.TryGetValue(HikeConstants.LOCATION_DEVICE_COORDINATE, out _myCoordinate);

            if (App.IS_TOMBSTONED)
            {
                _selectedCoordinate = PhoneApplicationService.Current.State[HikeConstants.LOCATION_COORDINATE] as GeoCoordinate;
                _searchString = PhoneApplicationService.Current.State[HikeConstants.LOCATION_SEARCH] as String;

                MyMap.ZoomLevel = (double)PhoneApplicationService.Current.State[HikeConstants.ZOOM_LEVEL];

                if (_selectedCoordinate == null && _myCoordinate == null)
                    GetCurrentCoordinate();
                else
                {
                    DrawMapMarkers();
                    GetMyPlaceDetails();

                    if (String.IsNullOrEmpty(_searchString))
                        GetPlaces();
                    else
                        Search();
                }
            }

            if (!_isLocationEnabled)
                return;

            if (_myCoordinate != null)
            {
                if (e.NavigationMode == System.Windows.Navigation.NavigationMode.New)
                {
                    GetCurrentCoordinate();

                    _selectedCoordinate = _myCoordinate;

                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            MyMap.SetView(_myCoordinate, 16, MapAnimationKind.Parabolic);
                            DrawMapMarkers();
                        });

                    GetMyPlaceDetails();

                    if (String.IsNullOrEmpty(_searchString))
                        GetPlaces();
                    else
                        Search();
                }
            }
            else
                GetCurrentCoordinate();

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.Back)
            {
                PhoneApplicationService.Current.State.Remove(HikeConstants.LOCATION_COORDINATE);
                PhoneApplicationService.Current.State.Remove(HikeConstants.LOCATION_SEARCH);
                PhoneApplicationService.Current.State.Remove(HikeConstants.ZOOM_LEVEL);
            }
            else
            {
                PhoneApplicationService.Current.State[HikeConstants.LOCATION_COORDINATE] = _selectedCoordinate;
                PhoneApplicationService.Current.State[HikeConstants.LOCATION_SEARCH] = _searchString;

                if (MyMap != null)
                    PhoneApplicationService.Current.State[HikeConstants.ZOOM_LEVEL] = MyMap.ZoomLevel;
                else
                    PhoneApplicationService.Current.State[HikeConstants.ZOOM_LEVEL] = 16;
            }

            if (_myCoordinate != null)
                App.WriteToIsoStorageSettings(HikeConstants.LOCATION_DEVICE_COORDINATE, _myCoordinate);

            base.OnNavigatedFrom(e);
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            shareIconButton.IsEnabled = false;
        }

        private void SearchTextBox_LostFocus_1(object sender, RoutedEventArgs e)
        {
            shareIconButton.IsEnabled = true;
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
                if (_vicinity != value)
                {
                    _vicinity = value == null ? String.Empty : value.Trim(new char[] { '\n', ' ' }).Replace("\n", ", ");
                    NotifyPropertyChanged("vicinity");
                    NotifyPropertyChanged("VicinityVisibility");
                }
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