using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Xml;
using System.Text.RegularExpressions;
using System.IO;
using Newtonsoft.Json.Linq;
using BingGeocoder;
namespace TravelDistance
{
    class DistanceCalc
    {
        public string Url;     

        public DistanceCalc()
        {
            if (!GetConfigFile().ExistINIFile())
            {
                Console.WriteLine("Configure file does not exist.");
            }
        }

        //Calling web service to return address GEO coding
        public List<GEOCodeStep> GetGEOCode(string address)
        {

            if (!GetConfigFile().ExistINIFile())
            {
                Console.WriteLine("Configure file does not exist.");
                return null;
            }
            else
            {
                //string requestUrl = configfile.IniReadValue("WebService", "WS_URL_DIR");
                //string requestUrl = configfile.IniReadValue("WebService", "WS_URL_GEO");
                //string apikey = configfile.IniReadValue("WebService", "WS_KEY");
                //var URL = string.Format(requestUrl, address, apikey);
                List<GEOCodeStep> geoCodeStepsList = new List<GEOCodeStep>();
                // Using Google GEO API
                //geoCodeStepsList = GetGoogleWebService(address, "WebService", "WS_URL_GEO", "WS_KEY");
                // Using Bing Maps GEO API
                //geoCodeStepsList = GetBingWebService(address, "WebService", "WS_URL_BING_GEO", "WS_BING_KEY");                
                // Using the package BingGeocoder 
                geoCodeStepsList = BingGeocoderList(address, "WebService", "WS_BING_KEY");        
                return geoCodeStepsList;
               
            }
        }

        //Calling package:BingGeocoder to return address GEO coding
        private static List<GEOCodeStep> BingGeocoderList(string address, string webService, string key)
        {
            var geoCodeStepsList = new List<GEOCodeStep>();
            string apikey = GetConfigFile().IniReadValue(webService, key);

            try
            {
                BingGeocoderClient bingClient = new BingGeocoderClient(apikey);
                BingGeocoderResult bingResult = new BingGeocoderResult();
                bingResult = bingClient.Geocode(address);
                if (bingResult.Latitude != null && bingResult.Longitude != null)
                {
                    var geoCodeSteps = new GEOCodeStep();
                    geoCodeSteps.Status = "OK";
                    geoCodeSteps.lat = bingResult.Latitude;
                    geoCodeSteps.lng = bingResult.Longitude;
                    geoCodeStepsList.Add(geoCodeSteps);
                }
                else
                {
                    var geoCodeSteps = new GEOCodeStep();
                    //geoCodeSteps.Status = respStatus;
                    geoCodeSteps.lat = "ZERO_RESULTS";
                    geoCodeSteps.lng = "ZERO_RESULTS";
                    geoCodeStepsList.Add(geoCodeSteps);
                }
            }
            catch (Exception ex)
            {
                RecordLog("BingGeocoderList", ex.Message);
                return null;
            }

            return geoCodeStepsList;
        }

        //Parsing response to get Google GEO Coding (lat and lng)
        private static List<GEOCodeStep> ParseGeoCodeResults(string result)
        {

            var geoCodeStepsList = new List<GEOCodeStep>();
            var xmlDoc = new XmlDocument { InnerXml = result };
            if (xmlDoc.HasChildNodes)
            {
                var geoCodeResponseNode = xmlDoc.SelectSingleNode("GeocodeResponse");
                if (geoCodeResponseNode != null)
                {
                    var statusNode = geoCodeResponseNode.SelectSingleNode("status");
                    var lat = geoCodeResponseNode.SelectSingleNode("result/geometry/location/lat");
                    var lng = geoCodeResponseNode.SelectSingleNode("result/geometry/location/lng");
                    if (statusNode.InnerText.Equals("OK"))
                    {
                        var locs = geoCodeResponseNode.SelectNodes("result/geometry/location");
                        var geoCodeSteps = new GEOCodeStep();
                        geoCodeSteps.Status = statusNode.InnerText;
                        geoCodeSteps.lat = lat.InnerText;
                        geoCodeSteps.lng = lng.InnerText;
                        geoCodeStepsList.Add(geoCodeSteps);
                        
                    }
                    else
                    {
                        var geoCodeSteps = new GEOCodeStep();
                        geoCodeSteps.Status = statusNode.InnerText;
                        geoCodeSteps.lat = "ZERO_RESULTS";
                        geoCodeSteps.lng = "ZERO_RESULTS";
                        geoCodeStepsList.Add(geoCodeSteps);
                    }
                }
            }
            return geoCodeStepsList;
        }

        private static List<GEOCodeStep> GetGoogleWebService(string address, string webService, string url, string key)
        {
            string requestUrl = GetConfigFile().IniReadValue(webService, url);
            string apikey = GetConfigFile().IniReadValue(webService, key);
            var URL = string.Format(requestUrl, address, apikey);
            try
            {
                var client = new WebClient();
                var result = client.DownloadString(URL);
                return ParseGeoCodeResults(result);
            }
            catch (Exception ex)
            {
                RecordLog("client.DownloadString", ex.Message);
                return null;
            }
        }
        
        //Parsing response to get BING GEO Coding (lat and lng)
        private static List<GEOCodeStep> ParseBingGeoCodeResults(string result)
        {
            var geoCodeStepsList = new List<GEOCodeStep>();

            try
            {
                var jsonObject = JObject.Parse(result);
                var respArray = (JArray)jsonObject["resourceSets"];
                var respStatus = jsonObject["statusCode"].ToString().Trim();

                var resultArray = (JArray)respArray[0]["resources"];
                if (respStatus == "200" && resultArray.Count >= 1)
                {
                    var geoCodeSteps = new GEOCodeStep();
                    var geoArray = (JArray)resultArray[0]["point"]["coordinates"];                
                    geoCodeSteps.Status = respStatus;
                    geoCodeSteps.lat = geoArray[0].ToString();
                    geoCodeSteps.lng = geoArray[1].ToString();
                    geoCodeStepsList.Add(geoCodeSteps);
                }
                else
                {
                    var geoCodeSteps = new GEOCodeStep();
                    geoCodeSteps.Status = respStatus;
                    geoCodeSteps.lat = "ZERO_RESULTS";
                    geoCodeSteps.lng = "ZERO_RESULTS";
                    geoCodeStepsList.Add(geoCodeSteps);
                }

            }
            catch (Exception e)
            {
                RecordLog("ParseBingGeoCodeResults", e.Message);
                return null;
            }

            return geoCodeStepsList;
        }

        private static List<GEOCodeStep> GetBingWebService(string address, string webService, string url, string key)
        {
            string requestUrl = GetConfigFile().IniReadValue(webService, url);
            string apikey = GetConfigFile().IniReadValue(webService, key);
            var URL = string.Format(requestUrl, address, apikey);

            try
            {
                var client = new WebClient();
                var result = client.DownloadString(URL);
                return ParseBingGeoCodeResults(result);
            }
            catch (Exception ex)
            {
                RecordLog("client.DownloadString", ex.Message);
                return null;
            }
        }        

        public List<DirectionStep> GetDirections(string origin, string destination)
        {
            string strcurpath = Directory.GetCurrentDirectory() + '\\' + "umass.ini";
            Inifile configfile = new Inifile(strcurpath);
            if (!configfile.ExistINIFile())
            {
                Console.WriteLine("Configure file does not exist.");
                return null;
            }
            else
            {
                string requestUrl = configfile.IniReadValue("WebService", "WS_URL_DIR");
                var URL = string.Format(requestUrl, origin, destination);
                try
                {
                    var client = new WebClient();
                    var result = client.DownloadString(URL);
                    return ParseDirectionResults(result);
                }
                catch (Exception ex)
                {
                    RecordLog("client.DownloadString", ex.Message);
                    return null;
                }
            }
        }

        private static List<DirectionStep> ParseDirectionResults(string result)
        {

            var directionStepsList = new List<DirectionStep>();
            var xmlDoc = new XmlDocument { InnerXml = result };
            if (xmlDoc.HasChildNodes)
            {
                var directionsResponseNode = xmlDoc.SelectSingleNode("DirectionsResponse");
                if (directionsResponseNode != null)
                {
                    var statusNode = directionsResponseNode.SelectSingleNode("status");
                    if (statusNode.InnerText.Equals("OK"))
                    {
                        var legs = directionsResponseNode.SelectNodes("route/leg");
                        foreach (XmlNode leg in legs)
                        {
                            int stepCount = 1;
                            var stepNodes = leg.SelectNodes("step");
                            var steps = new List<DirectionStep>();
                            foreach (XmlNode stepNode in stepNodes)
                            {
                                var directionStep = new DirectionStep();
                                directionStep.Index = stepCount++;
                                directionStep.DistanceMI = stepNode.SelectSingleNode("distance/text").InnerText;
                                directionStep.DistanceKM = stepNode.SelectSingleNode("distance/value").InnerText;

                                directionStep.Description = Regex.Replace(stepNode.SelectSingleNode("html_instructions").InnerText, "<[^<]+?>", "");
                                steps.Add(directionStep);
                            }

                            var directionSteps = new DirectionStep(); 
                            directionSteps.Status = statusNode.InnerText;
                            directionSteps.DistanceMI = leg.SelectSingleNode("distance/text").InnerText;
                            directionSteps.DistanceValue = Convert.ToInt32(leg.SelectSingleNode("distance/value").InnerText);
                            directionSteps.DistanceKM = Math.Round((Convert.ToInt32(directionSteps.DistanceValue) / 1000 / 1.0000), 1).ToString("#,0") + " km";
                            directionStepsList.Add(directionSteps);
                        }

                    }
                    else
                    {
                        var directionSteps = new DirectionStep();    
                        directionSteps.Status = statusNode.InnerText;
                        directionSteps.DistanceValue = 0;
                        directionSteps.DistanceMI = "N/A";
                        directionSteps.DistanceKM = "N/A";
                        directionStepsList.Add(directionSteps);
                    }
                }
            }
            return directionStepsList;
        }

        private static Inifile GetConfigFile()
        {
            string strcurpath = Directory.GetCurrentDirectory() + '\\' + "umass.ini";
            Inifile configfile = new Inifile(strcurpath);
            return configfile;
        }

        public static void RecordLog(string funcName, string logText)
        {
            // Create a writer and open the file:
            StreamWriter log;
            string errorLogFile = Directory.GetCurrentDirectory() + '\\' + "TravelDistanceLog.txt";


            if (!File.Exists(errorLogFile))
            {
                log = new StreamWriter(errorLogFile);
            }
            else
            {
                log = File.AppendText(errorLogFile);
            }

            // Write to the file:
            log.WriteLine(DateTime.Now);
            log.WriteLine(funcName + " - " + logText);
            log.WriteLine();

            // Close the stream:
            log.Close();
        }
    }
}
