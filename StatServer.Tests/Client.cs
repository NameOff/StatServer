using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using System.Web;

namespace StatServer.Tests
{
    class Client
    {
        public readonly string Prefix;
        public Client(string prefix)
        {
            Prefix = prefix;
        }

        private static HttpResponse GetAnswer(WebRequest request)
        {
            try
            {
                var httpResponse = (HttpWebResponse)request.GetResponse();
                var statusCode = httpResponse.StatusCode;
                string result;
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    result = streamReader.ReadToEnd();
                

                return new HttpResponse((HttpResponse.Status)statusCode, result);
            }
            catch (WebException)
            {
                return new HttpResponse(HttpResponse.Status.NotFound);
            }
        }

        private HttpResponse SendGetRequest(string uri)
        {
            var httpWebRequest =
                (HttpWebRequest)WebRequest.Create($"{Prefix}{uri}");
            httpWebRequest.Method = "GET";

            return GetAnswer(httpWebRequest);
        }

        private HttpResponse SendPutRequest(string uri, string json)
        {
            var httpWebRequest =
                (HttpWebRequest)WebRequest.Create($"{Prefix}{uri}");
            httpWebRequest.Method = "PUT";
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();
            }
            return GetAnswer(httpWebRequest);
        }

        public Client SendRequest()
        {
            return this;
        }

        public HttpResponse PutMatchStats(GameMatchStats stats, string endpoint, DateTime timestamp)
        {
            var stringTimestamp = $"{timestamp:s}Z";
            var uri = $"servers/{endpoint}/matches/{stringTimestamp}";
            var json = JsonConvert.SerializeObject(stats);
            return SendPutRequest(uri, json);
        }

        public HttpResponse PutServerInfo(GameServerInfo info, string endpoint)
        {
            var uri = $"servers/{endpoint}/info";
            var json = JsonConvert.SerializeObject(info);
            return SendPutRequest(uri, json);
        }

        public HttpResponse GetMatchStats(string endpoint, DateTime timestamp)
        {
            var stringTimestamp = $"{timestamp:s}Z";
            var uri = $"servers/{endpoint}/matches/{stringTimestamp}";
            return SendGetRequest(uri);
        }

        public HttpResponse GetServerInfo(string endpoint)
        {
            var uri = $"servers/{endpoint}/info";
            return SendGetRequest(uri);
        }

        public HttpResponse GetAllServersInfo()
        {
            var uri = "servers/info";
            return SendGetRequest(uri);
        }

        public HttpResponse GetServerStats(string endpoint)
        {
            var uri = $"servers/{endpoint}/stats";
            return SendGetRequest(uri);
        }

        public HttpResponse GetPlayerStats(string name)
        {
            var encodedName = HttpUtility.UrlEncode(name);
            var uri = $"players/{encodedName}/stats";
            return SendGetRequest(uri);
        }

        public HttpResponse GetRecentMatches(int count)
        {
            var uri = $"reports/recent-matches/{count}";
            return SendGetRequest(uri);
        }

        public HttpResponse GetBestPlayers(int count)
        {
            var uri = $"reports/best-players/{count}";
            return SendGetRequest(uri);
        }

        public HttpResponse GetPopularServers(int count)
        {
            var uri = $"reports/popular-servers/{count}";
            return SendGetRequest(uri);
        }
    }
}
