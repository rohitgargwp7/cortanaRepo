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
using System.Web;
using System.Net.NetworkInformation;
using System.Globalization;

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
        private GeoCoordinate _customCoordinate = null;

        private string _nokiaPlacesUrl = "http://places.nlp.nokia.com/places/v1/discover/explore";
        private string _nokiaSeacrhUrl = "http://places.nlp.nokia.com/places/v1/discover/search";
        private string _nokiaPlacesAppID = "KyG8YRrgO09I7bm9jXwd";
        private string _nokiaPlacesAppCode = "Uhjk-Gny7_A-ISaJb3DMKQ";
        private List<Place> _places;
        Place _selectedPlace;
        Place _myPlace;
        Boolean _isMapTapped = false;
        Boolean _isLocationEnabled = true;
        Boolean _isDefaultLocationCall = true;
        String _cgen = HikeConstants.NokiaHere.CGEN_GPS;
        String _resultString = String.Empty;
        Int32 _selectedIndex = 0;

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

        void GetPlaces()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                return;

            placesListGrid.Visibility = Visibility.Collapsed;
            loadingPlaces.IsIndeterminate = true;

            string url = string.Format("{0}?at={1},{2};cgen={3}&app_id={4}&app_code={5}&tf=plain&pretty=true", _nokiaPlacesUrl, _selectedCoordinate.Latitude.ToString("0.000000", CultureInfo.InvariantCulture), _selectedCoordinate.Longitude.ToString("0.000000", CultureInfo.InvariantCulture), _cgen, _nokiaPlacesAppID, _nokiaPlacesAppCode);
            AccountUtils.createNokiaPlacesGetRequest(url, new AccountUtils.postResponseFunction(PlacesResult_Callback));
        }

        void PlacesResult_Callback(JObject obj)
        {
            if (MyMap == null)
                return;

            _resultString = obj == null ? String.Empty : obj.ToString(Newtonsoft.Json.Formatting.None);

            PopulatePlaces(obj);
        }

        private void PopulatePlaces(JObject obj, int selectedIndex = 0)
        {
            _places = this.ParsePlaces(obj);

            Deployment.Current.Dispatcher.BeginInvoke(new Action<int>(delegate(int index)
            {
                if (MyMap == null)
                    return;

                if (_selectedPlace == null)
                {
                    try
                    {
                        JToken jToken = obj[HikeConstants.NokiaHere.SEARCH][HikeConstants.NokiaHere.CONTEXT][HikeConstants.NokiaHere.LOCATION][HikeConstants.NokiaHere.ADDRESS];
                        _myPlaceVicinity = jToken[HikeConstants.NokiaHere.TEXT].ToString().Replace("\n", ", ");
                    }
                    catch
                    {
                        _myPlaceVicinity = String.Empty;
                    }

                    if (_myPlace == null)
                    {
                        _myPlace = new Place()
                        {
                            position = _selectedCoordinate,
                            title = _myCoordinate == null || _selectedCoordinate != _myCoordinate ? AppResources.Location_Txt : AppResources.My_Location_Text,
                            vicinity = _myPlaceVicinity,
                            icon = String.Empty
                        };
                    }

                    if (!_places.Contains(_myPlace))
                        _places.Insert(0, _myPlace);
                }
                else
                {
                    if (!_places.Contains(_selectedPlace))
                        _places.Insert(0, _selectedPlace);
                }

                _selectedIndex = index;
                _selectedPlace = _places[index];
                PlacesList.ItemsSource = _places;
                PlacesList.SelectedItem = _places[index];
                
                UpdateLayout();
                PlacesList.ScrollIntoView(PlacesList.SelectedItem);
                UpdateLayout();

                placesListGrid.Visibility = Visibility.Visible;
                loadingPlaces.IsIndeterminate = false;
                DrawMapMarkers();
            }), selectedIndex);
        }

        private List<Place> ParsePlaces(JObject json)
        {
            List<Place> places = new List<Place>();
            if (json == null)
                return places;
            try
            {
                JToken jToken = json[HikeConstants.NokiaHere.RESULTS][HikeConstants.NokiaHere.ITEMS];
                if (jToken == null)
                    return places;

                var jlList = jToken.Children();
                foreach (JToken token in jlList)
                {
                    Place place = JsonConvert.DeserializeObject<Place>(token.ToString());

                    if (place.type == HikeConstants.NokiaHere.PLACE_TYPE)
                    {
                        var jTokenListPosition = token[HikeConstants.NokiaHere.POSITION];
                        GeoCoordinate position = new GeoCoordinate();
                        position.Latitude = (double) jTokenListPosition.First;
                        position.Longitude = (double) jTokenListPosition.Last;
                        place.position = position;
                        places.Add(place);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ShareLocation :: ParsePlaces : ParsePlaces , Exception : " + ex.StackTrace);
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

                if (_selectedPlace != null)
                    DrawMapMarker(_selectedPlace, mapLayer);
                else if (_selectedCoordinate != null)
                    DrawMapMarker(_selectedCoordinate, mapLayer);

                MyMap.Layers.Add(mapLayer);
            }
        }

        private void DrawMapMarker(GeoCoordinate coordinate, MapLayer mapLayer)
        {
            // Create a map marker
            Image polygon = new Image() { MaxHeight = 42, MaxWidth = 42 };

            polygon.Source = UI_Utils.Instance.MyLocationPin;

            // Enable marker to be tapped for location information
            polygon.Tag = new GeoCoordinate(coordinate.Latitude, coordinate.Longitude);

            // Create a MapOverlay and add marker.
            MapOverlay overlay = new MapOverlay();
            overlay.Content = polygon;
            overlay.GeoCoordinate = new GeoCoordinate(coordinate.Latitude, coordinate.Longitude);
            overlay.PositionOrigin = new System.Windows.Point(0.0, 1.0);
            mapLayer.Add(overlay);
        }

        private void DrawMapMarker(Place place, MapLayer mapLayer)
        {
            // Create a map marker
            Image polygon = new Image() { MaxHeight = 42, MaxWidth = 42 };

            polygon.Source = place.PlaceImage;

            // Enable marker to be tapped for location information
            polygon.Tag = new GeoCoordinate(place.position.Latitude, place.position.Longitude);

            // Create a MapOverlay and add marker.
            MapOverlay overlay = new MapOverlay();
            overlay.Content = polygon;
            overlay.GeoCoordinate = new GeoCoordinate(place.position.Latitude, place.position.Longitude);
            overlay.PositionOrigin = new System.Windows.Point(0.0, 1.0);
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
            placesListGrid.Visibility = Visibility.Collapsed;
            loadingPlaces.IsIndeterminate = true;

            GeoCoordinate newCoordinate = null;

            _geolocator.DesiredAccuracyInMeters = 10;
            _geolocator.MovementThreshold = 5;
            _geolocator.DesiredAccuracy = PositionAccuracy.High;

            IAsyncOperation<Geoposition> locationTask = _geolocator.GetGeopositionAsync(TimeSpan.FromMinutes(10), TimeSpan.FromSeconds(5));

            try
            {
                Geoposition currentPosition = await locationTask;

                if (_isMapTapped && _isDefaultLocationCall)
                {
                    _isFetchingCurrentLocation = false;
                    _isDefaultLocationCall = false;
                    HideProgressIndicator();
                    placesListGrid.Visibility = Visibility.Visible;
                    loadingPlaces.IsIndeterminate = false;
                    return;
                }

                var latitutde = Math.Round(currentPosition.Coordinate.Latitude, 6);
                var longitute = Math.Round(currentPosition.Coordinate.Longitude, 6);
                newCoordinate = new GeoCoordinate(latitutde, longitute);

                _selectedCoordinate = newCoordinate;

                _cgen = HikeConstants.NokiaHere.CGEN_GPS;
                _isFetchingCurrentLocation = false;
                HideProgressIndicator();

                if (_myCoordinate != newCoordinate || _isMapTapped || _places == null)
                {
                    _myCoordinate = newCoordinate;
                    _isMapTapped = false;
                    GetPlaces();
                }
                else
                {
                    placesListGrid.Visibility = Visibility.Visible;
                    loadingPlaces.IsIndeterminate = false;
                }

                Dispatcher.BeginInvoke(() =>
                {
                    shareIconButton.IsEnabled = true;

                    DrawMapMarkers();
                    MyMap.SetView(_myCoordinate, 16, MapAnimationKind.Parabolic);
                });
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

            if (_progressIndicator != null)
            {
                _progressIndicator.IsVisible = false;
                SystemTray.SetProgressIndicator(this, _progressIndicator);
            }
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

            var title = _selectedPlace == null ? String.Empty : _selectedPlace.title.Contains(AppResources.Location_Txt) ? String.Empty : _selectedPlace.title;

            singleFileInfo[HikeConstants.FILE_NAME] = title;
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
            _cgen = HikeConstants.NokiaHere.CGEN_MAP;
            System.Windows.Point p = e.GetPosition(this.MyMap);
            GeoCoordinate geo = new GeoCoordinate();
            geo = MyMap.ConvertViewportPointToGeoCoordinate(p);
            MyMap.SetView(geo, MyMap.ZoomLevel, MapAnimationKind.Parabolic);
            _selectedCoordinate = _customCoordinate = geo;
            _selectedPlace = null;
            _myPlace = null;
            shareIconButton.IsEnabled = true;

            PlacesGrid.Visibility = NetworkInterface.GetIsNetworkAvailable() ? PlacesGrid.Visibility = Visibility.Visible : PlacesGrid.Visibility = Visibility.Collapsed;

            if (String.IsNullOrEmpty(_searchString))
                GetPlaces();
            else
                Search();
            
            DrawMapMarkers();
        }

        private byte[] captureThumbnail()
        {
            byte[] thumbnailBytes = null;

            var point = MyMap.ConvertGeoCoordinateToViewportPoint(_selectedCoordinate);
            point.X -= HikeConstants.LOCATION_THUMBNAIL_MAX_WIDTH / 2;
            point.Y -= HikeConstants.LOCATION_THUMBNAIL_MAX_HEIGHT / 2;

            var screenshot = new WriteableBitmap(MyMap, new TranslateTransform());

            var newScreenshot = screenshot.Crop((int)point.X, (int)point.Y, HikeConstants.LOCATION_THUMBNAIL_MAX_WIDTH, HikeConstants.LOCATION_THUMBNAIL_MAX_HEIGHT);

            if (newScreenshot.Pixels.Length != 0)
                screenshot = newScreenshot;

            screenshot.Invalidate();

            using (MemoryStream ms = new MemoryStream())
            {
                screenshot.SaveJpeg(ms, HikeConstants.LOCATION_THUMBNAIL_MAX_WIDTH, HikeConstants.LOCATION_THUMBNAIL_MAX_HEIGHT, 0, 100);

                thumbnailBytes = ms.ToArray();
            }

            return thumbnailBytes;
        }

        private void PlacesList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var listbox = sender as ListBox;
            if (listbox != null)
            {
                var place = listbox.SelectedItem as Place;

                if (place != null)
                {
                    this.DataContext = place;
                    _selectedPlace = place;
                    _selectedCoordinate = place.position;

                    _selectedIndex = listbox.SelectedIndex;

                    MyMap.SetView(_selectedCoordinate, MyMap.ZoomLevel, MapAnimationKind.Parabolic);

                    DrawMapMarkers();
                    shareIconButton.IsEnabled = true;
                }
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
        Place _lastSelectedPlace = null;

        private void SearchAction_Tap(object sender, EventArgs e)
        {
            var searchString = SearchTextBox.Text.Trim();

            if (_searchString == searchString && _selectedPlace == _lastSelectedPlace) // avoid duplicated search calls for same place
                return;

            _lastSelectedPlace = _selectedPlace;
            _searchString = searchString;
            _resultString = String.Empty;

            this.Focus();

            if (_selectedPlace != _myPlace)
                _myPlace = null;

            if (String.IsNullOrEmpty(_searchString))
                GetPlaces();
            else
                Search();
        }

        void Search()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBox.Show(AppResources.No_Network_Txt, AppResources.NetworkError_TryAgain, MessageBoxButton.OK);
                return;
            }

            placesListGrid.Visibility = Visibility.Collapsed;
            loadingPlaces.IsIndeterminate = true;

            string url = string.Format("{0}?q={1}&at={2},{3};cgen={4}&app_id={5}&app_code={6}&tf=plain&pretty=true", _nokiaSeacrhUrl, HttpUtility.UrlEncode(_searchString), _selectedCoordinate.Latitude.ToString("0.000000", CultureInfo.InvariantCulture), _selectedCoordinate.Longitude.ToString("0.000000", CultureInfo.InvariantCulture), _cgen, _nokiaPlacesAppID, _nokiaPlacesAppCode);
            AccountUtils.createNokiaPlacesGetRequest(url, new AccountUtils.postResponseFunction(PlacesResult_Callback));
        }

        private void MyLocation_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (shareIconButton.IsEnabled == false || !_isLocationEnabled)
                return; 
            
            if (_myPlace != null && _myPlace.title == AppResources.My_Location_Text)
            {
                if (_selectedPlace != _myPlace)
                {
                    _selectedPlace = _myPlace;
                    PlacesList.SelectedItem = _myPlace;
                    UpdateLayout();
                    PlacesList.ScrollIntoView(_myPlace);
                }
                
                return;
            }

            _customCoordinate = null;
            shareIconButton.IsEnabled = false;
            _selectedPlace = null;
            _myPlace = null;

            PlacesGrid.Visibility = NetworkInterface.GetIsNetworkAvailable() ? PlacesGrid.Visibility = Visibility.Visible : PlacesGrid.Visibility = Visibility.Collapsed;

            GetCurrentCoordinate();
        }

        private void Places_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBox.Show(AppResources.No_Network_Txt, AppResources.NetworkError_TryAgain, MessageBoxButton.OK);
                return;
            }

            if (shareIconButton.IsEnabled == false)
                return;

            PlacesGrid.Visibility = PlacesGrid.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }

        protected override async void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (_geolocator == null)
                _geolocator = new Geolocator();

            if (_geolocator.LocationStatus == PositionStatus.Disabled)
            {
                var result = MessageBox.Show(AppResources.ShareLocation_LocationServiceNotEnabled_Txt, AppResources.Location_Heading, MessageBoxButton.OKCancel);

                if(result == MessageBoxResult.OK)
                    await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-location:"));

                _isLocationEnabled = false;
            }
            else if (App.appSettings.TryGetValue<bool>(App.USE_LOCATION_SETTING, out _isLocationEnabled))
            {
                var result = MessageBox.Show(AppResources.ShareLocation_LocationSettingsNotEnabled_Txt, AppResources.Location_Heading, MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.OK)
                {
                    App.appSettings.Remove(App.USE_LOCATION_SETTING);
                    App.appSettings.Save();
                    _isLocationEnabled = true;
                }
            }
            else
            {
                _isLocationEnabled = true;

                if (e.NavigationMode != System.Windows.Navigation.NavigationMode.New && _myCoordinate == null && !App.IS_TOMBSTONED)
                    GetCurrentCoordinate();
            }

            App.appSettings.TryGetValue(HikeConstants.LOCATION_DEVICE_COORDINATE, out _myCoordinate);

            if (App.IS_TOMBSTONED)
            {
                _isDefaultLocationCall = false;

                _customCoordinate = State[HikeConstants.LOCATION_MAP_COORDINATE] as GeoCoordinate;
                _searchString = State[HikeConstants.LOCATION_SEARCH] as String;
                _resultString = (String)State[HikeConstants.LOCATION_PLACE_SEARCH_RESULT];
                _selectedIndex = (Int32)State[HikeConstants.LOCATION_SELECTED_INDEX];

                MyMap.ZoomLevel = (double)State[HikeConstants.ZOOM_LEVEL];

                _selectedCoordinate = _customCoordinate == null ? _myCoordinate : _customCoordinate;
                PlacesGrid.Visibility = NetworkInterface.GetIsNetworkAvailable() ? PlacesGrid.Visibility = Visibility.Visible : PlacesGrid.Visibility = Visibility.Collapsed;

                if (_myCoordinate == null && _isLocationEnabled)
                    GetCurrentCoordinate();
                else if (_selectedCoordinate != null)
                {
                    shareIconButton.IsEnabled = true;
                    DrawMapMarkers();
                    SearchTextBox.Text = _searchString;

                    if (String.IsNullOrEmpty(_resultString))
                    {
                        if (String.IsNullOrEmpty(_searchString))
                            GetPlaces();
                        else
                            Search();
                    }
                    else
                    {
                        placesListGrid.Visibility = Visibility.Collapsed;
                        loadingPlaces.IsIndeterminate = true;
                        PopulatePlaces(JObject.Parse(_resultString), _selectedIndex);
                    }
                }
            }
            else if (e.NavigationMode == System.Windows.Navigation.NavigationMode.New && _isLocationEnabled)
            {
                GetCurrentCoordinate(); // get current coordinate and load last catched coordinate if its not null

                if (_myCoordinate != null)
                {
                    _selectedCoordinate = _myCoordinate;

                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            MyMap.SetView(_myCoordinate, 16, MapAnimationKind.Parabolic);
                            DrawMapMarkers();
                        });
                }
            }

            if (NetworkInterface.GetIsNetworkAvailable())
                PlacesGrid.Visibility = Visibility.Visible;
            else
                MessageBox.Show(AppResources.No_Network_Txt, AppResources.NetworkError_TryAgain, MessageBoxButton.OK);

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.Back)
            {
                State.Remove(HikeConstants.LOCATION_MAP_COORDINATE);
                State.Remove(HikeConstants.LOCATION_SEARCH);
                State.Remove(HikeConstants.ZOOM_LEVEL);
                State.Remove(HikeConstants.LOCATION_PLACE_SEARCH_RESULT);
                State.Remove(HikeConstants.LOCATION_SELECTED_INDEX);
            }
            else
            {
                State[HikeConstants.LOCATION_MAP_COORDINATE] = _customCoordinate;
                State[HikeConstants.LOCATION_SEARCH] = _searchString;

                if (MyMap != null)
                    State[HikeConstants.ZOOM_LEVEL] = MyMap.ZoomLevel;
                else
                    State[HikeConstants.ZOOM_LEVEL] = 16;

                State[HikeConstants.LOCATION_PLACE_SEARCH_RESULT] = _resultString;
                State[HikeConstants.LOCATION_SELECTED_INDEX] = _selectedIndex;
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

        ImageSource _placeImage;
        public ImageSource PlaceImage
        {
            get
            {
                if (_placeImage == null)
                {
                    _placeImage = ProcesImageSource();
                    return _placeImage;
                }
                else
                    return _placeImage;
            }
        }

        private ImageSource ProcesImageSource()
        {
            ImageSource source = null;

            source = new BitmapImage(new Uri("/view/images/MyLocation.png", UriKind.Relative));

            if (!String.IsNullOrEmpty(icon))
                ImageLoader.Load(source as BitmapImage, new Uri(icon), null, Utils.ConvertUrlToFileName(icon), true);

            return source;
        }


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