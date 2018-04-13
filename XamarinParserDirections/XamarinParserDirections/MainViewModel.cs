using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Net.Http;
using Newtonsoft.Json;
using Xamarin.Forms.Maps;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Net;
using System.Collections.ObjectModel;

namespace XamarinParserDirections
{
    public class MainViewModel : ContentPage, INotifyPropertyChanged
    {
        public MainViewModel()
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public string ApiAddressWithoutWaypoints = "https://maps.googleapis.com/maps/api/directions/json?key={0}&origin={1}&destination={2}"; //change later
        public string ApiAddressWithWaypoints = "https://maps.googleapis.com/maps/api/directions/json?key={0}&origin={1}&destination={2}&waypoints=optimize:true{3}"; //change later


        public string ApiKey = "AIzaSyAfP9wu3t4g0y3-UQE6sy5W6wFF03w9Cd0";

        private string _origin = "Киев, Дорогожицкая, 3";
        public string Origin
        {
            get
            {
                return _origin;
            }
            set
            {
                _origin = value;
                RaisePropertyChanged();
            }
        }

        private string _destination;
        public string Destination
        {
            get
            {
                return _destination;
            }
            set
            {
                _destination = value;
                RaisePropertyChanged();
            }
        }

        //дополнительно для промежуточных waypoints 

        private string _additionalWaypoint;
        public string AdditionalWaypoint
        {
            get
            {
                return _additionalWaypoint;
            }
            set
            {
                _additionalWaypoint = value;
                RaisePropertyChanged();
            }
        }

        private List<string> _waypointsList = new List<string>();
        public List<string> WaypointsList
        {
            get
            {
                return _waypointsList;
            }
            set
            {
                _waypointsList = value;
                RaisePropertyChanged();
            }
        }


        public Command AddWaypoint
        {
            get
            {
                return new Command(() =>
               {
                   if (string.IsNullOrEmpty(AdditionalWaypoint))
                       return;
                   
                   WaypointsList.Add(AdditionalWaypoint);

                   AdditionalWaypoint = String.Empty;
               });
            }
        }


        private string _queryText;
        public string QueryText //для отбора в результатах
        {
            get
            {
                return _queryText;
            }
            set
            {
                _queryText = value;
                ResultContains(_queryText);
                RaisePropertyChanged();

            }
        }

        //дополнительно

        //public List<string> OriginList { get; set; } = new List<string>
        //    {

        //    };

        public List<string> DestinationList { get; set; } = new List<string>
            {
            "Киев, Золотые Ворота",
            "Бровары",
            "Киев, Харьковская",
            "Борисполь",
            "Киев, Петровка",
            "Киев, Оболонская площадь",
            "Киев, Ватутина, 3",
            "Киев, Григоренка, 24"

            };


        //дополнительное задание
        public void ResultContains(string query)
        {


            if (string.IsNullOrEmpty(QueryText))
                return;

            PartialFullInfoList = new ObservableCollection<FullInfo>(CompleteFullInfoList?.Where(x => x.DestinationAddress.Contains(query) || x.DestinationLatitude.ToString().Contains(query) ||
                                                      x.DestinationLongitude.ToString().Contains(query) || x.DistanceKilometers.Contains(query) || x.DistanceMeters.ToString().Contains(query) ||
                                                      x.DurationHours.Contains(query) || x.DurationSeconds.ToString().Contains(query) || x.OriginAddress.Contains(query) ||
                                                      x.OriginLatitude.ToString().Contains(query) || x.OriginLongitude.ToString().Contains(query)).ToList());

        }


        public ObservableCollection<FullInfo> _completefullInfoList = new ObservableCollection<FullInfo>();
        public ObservableCollection<FullInfo> CompleteFullInfoList
        {
            get
            {
                return _completefullInfoList;
            }
            set
            {
                _completefullInfoList = value;
                //PartialFullInfoList = _completefullInfoList;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<FullInfo> _partialFullInfoList = new ObservableCollection<FullInfo>();
        public ObservableCollection<FullInfo> PartialFullInfoList
        {
            get
            {
                return _partialFullInfoList;
            }
            set
            {
                _partialFullInfoList = value;
                RaisePropertyChanged();
            }
        }










        HttpClient Client { get; set; } = new HttpClient();


        public Command GetInfoWithoutWaypointsCommand
        {
            get
            {

                return new Command<List<string>>(async (List<string> DestinationList) =>

              {

                  foreach (var destination in DestinationList)
                  {
                      using (HttpResponseMessage message = await Client.GetAsync(string.Format(ApiAddressWithoutWaypoints, ApiKey, WebUtility.UrlEncode(Origin), WebUtility.UrlEncode(destination))).ConfigureAwait(false))
                      {
                          if (message.IsSuccessStatusCode)
                          {
                              string json = await message.Content.ReadAsStringAsync().ConfigureAwait(false);

                              RootObject results = JsonConvert.DeserializeObject<RootObject>(json); // add Task run - await, because it goes past this point before the data is fetched otherwise;

                              if (results.status == "OK")
                              {
                                  foreach (var route in results.routes) //вложенные уровни JSON, иначе не работает десериализация результата от этого API (результат содержит вложенные списки элементов)ы
                                  {
                                      foreach (var leg in route.legs)
                                      {
                                          var fullInfo = new FullInfo
                                          {
                                              DistanceKilometers = leg.distance.text,//.distance.text,
                                              DistanceMeters = leg.distance.value,
                                              DurationHours = leg.duration.text,
                                              DurationSeconds = leg.duration.value,
                                              OriginAddress = leg.start_address,
                                              DestinationAddress = leg.end_address,
                                              OriginLatitude = leg.start_location.lat,
                                              OriginLongitude = leg.start_location.lng,
                                              DestinationLatitude = leg.end_location.lat,
                                              DestinationLongitude = leg.end_location.lng,
                                          };

                                          CompleteFullInfoList.Add(fullInfo);
                                          PartialFullInfoList = new ObservableCollection<FullInfo>(CompleteFullInfoList); // если просто присваивать одно другому, меняются оба, так как это ссылочный тип
                                      }
                                  }
                              }
                          }
                      }
                  }


              });
            }
        }


        //google directions additional waypoints

        public string ConvertWaypoints(List<string> waypointsList)
        {

            string newWaypointString = String.Empty; ; //= string.Join( overload with list and separator, but the last waypoint shouldn't have a separtor at the end;

            foreach (var item in waypointsList)
            {
                newWaypointString += "|" + item;
            }

            return newWaypointString;

        }





        public Command GetInfoWithWaypointsCommand
        {
            get
            {

                return new Command(async () => // return new Command<List<string>>(async (List<string> DestinationList) =>

                {
                    if (WaypointsList == null)
                        return;

                   var waypoints = ConvertWaypoints(WaypointsList);

                    //foreach (var destination in DestinationList)
                    //{
                        using (HttpResponseMessage message = await Client.GetAsync(string.Format(ApiAddressWithWaypoints, ApiKey, WebUtility.UrlEncode(Origin), WebUtility.UrlEncode(Destination), WebUtility.UrlEncode(waypoints))).ConfigureAwait(false))
                        {
                            if (message.IsSuccessStatusCode)
                            {
                                string json = await message.Content.ReadAsStringAsync().ConfigureAwait(false);

                                RootObject results = JsonConvert.DeserializeObject<RootObject>(json); // add Task run - await, because it goes past this point before the data is fetched otherwise;

                                if (results.status == "OK")
                                {
                                    foreach (var route in results.routes) //вложенные уровни JSON, иначе не работает десериализация результата от этого API (результат содержит вложенные списки элементов)ы
                                    {
                                        foreach (var leg in route.legs)
                                        {
                                            var fullInfo = new FullInfo
                                            {
                                                DistanceKilometers = leg.distance.text,//.distance.text,
                                                DistanceMeters = leg.distance.value,
                                                DurationHours = leg.duration.text,
                                                DurationSeconds = leg.duration.value,
                                                OriginAddress = leg.start_address,
                                                DestinationAddress = leg.end_address,
                                                OriginLatitude = leg.start_location.lat,
                                                OriginLongitude = leg.start_location.lng,
                                                DestinationLatitude = leg.end_location.lat,
                                                DestinationLongitude = leg.end_location.lng,
                                            };

                                            CompleteFullInfoList.Add(fullInfo);
                                            PartialFullInfoList = new ObservableCollection<FullInfo>(CompleteFullInfoList); // если просто присваивать одно другому, меняются оба, так как это ссылочный тип
                                        }
                                    }
                                }
                            }
                        }
                    
                    WaypointsList.Clear();

                });

               
            }
        }



    }

    //json2sharp conversion

    #region json2sharp code 

    public class GeocodedWaypoint
    {
        public string geocoder_status { get; set; }
        public string place_id { get; set; }
    }

    public class Distance
    {
        public string text { get; set; }
        public int value { get; set; }
    }

    public class Duration
    {
        public string text { get; set; }
        public int value { get; set; }
    }

    public class EndLocation
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }

    public class StartLocation
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }

    public class Leg
    {
        public Distance distance { get; set; }
        public Duration duration { get; set; }
        public string end_address { get; set; }
        public EndLocation end_location { get; set; }
        public string start_address { get; set; }
        public StartLocation start_location { get; set; }
    }

    public class Route
    {
        [Newtonsoft.Json.JsonProperty("legs")]
        public List<Leg> legs { get; set; }// public List<Leg> legs { get; set; }

        public List<object> waypoint_order { get; set; }
    }

    public class RootObject
    {
        public List<GeocodedWaypoint> geocoded_waypoints { get; set; }

        [Newtonsoft.Json.JsonProperty("routes")]
        public List<Route> routes { get; set; } //  public List<Route> routes { get; set; }

        public string status { get; set; }
    }

    //public class ResultList
    //{
    //    public List<RootObject> Results { get; set; }

    //}



    #endregion



    //new resulting list


    public class FullInfo
    {
        public string DistanceKilometers { get; set; }

        public int DistanceMeters { get; set; }

        public string DurationHours { get; set; }

        public int DurationSeconds { get; set; }

        public string Place_id { get; set; }

        public string OriginAddress { get; set; }

        public string DestinationAddress { get; set; }

        public double OriginLatitude { get; set; }

        public double OriginLongitude { get; set; }

        public double DestinationLatitude { get; set; }

        public double DestinationLongitude { get; set; }

        public string Description => ToString();

        public override string ToString()
        {
            return $"Origin address {OriginAddress}, Destination address {DestinationAddress}, Distance {DistanceKilometers}, Distance in meters {DistanceMeters}m, Travel duration {DurationHours}, Travel duration in seconds {DurationSeconds}sec, " +
                $"Origin location ({OriginLatitude}, {OriginLongitude}), Destination location ({DestinationLatitude}, {DestinationLongitude})";
        }


    }

}
