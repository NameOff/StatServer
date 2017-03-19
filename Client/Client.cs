using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using System.Web;
using StatServer;

namespace Client
{
    public class Client
    {
        public readonly string Prefix;
        public Client(string prefix)
        {
            Prefix = prefix;
        }

        private static Response GetAnswer(WebRequest request)
        {
            try
            {
                var Response = (HttpWebResponse)request.GetResponse();
                var statusCode = Response.StatusCode;
                string result;
                using (var streamReader = new StreamReader(Response.GetResponseStream()))
                    result = streamReader.ReadToEnd();


                return new Response((Response.Status)statusCode, result);
            }
            catch (WebException)
            {
                return new Response(Response.Status.NotFound);
            }
        }

        private Response SendGetRequest(string uri)
        {
            var httpWebRequest =
                (HttpWebRequest)WebRequest.Create($"{Prefix}{uri}");
            httpWebRequest.Method = "GET";

            return GetAnswer(httpWebRequest);
        }

        private Response SendPutRequest(string uri, string json)
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

        public Response PutMatchStats(GameMatchStats stats, string endpoint, DateTime timestamp)
        {
            var stringTimestamp = $"{timestamp:s}Z";
            var uri = $"servers/{endpoint}/matches/{stringTimestamp}";
            var json = JsonConvert.SerializeObject(stats);
            return SendPutRequest(uri, json);
        }

        public Response PutServerInfo(GameServerInfo info, string endpoint)
        {
            var uri = $"servers/{endpoint}/info";
            var json = JsonConvert.SerializeObject(info);
            return SendPutRequest(uri, json);
        }

        public Response GetMatchStats(string endpoint, DateTime timestamp)
        {
            var stringTimestamp = $"{timestamp:s}Z";
            var uri = $"servers/{endpoint}/matches/{stringTimestamp}";
            return SendGetRequest(uri);
        }

        public Response GetServerInfo(string endpoint)
        {
            var uri = $"servers/{endpoint}/info";
            return SendGetRequest(uri);
        }

        public Response GetAllServersInfo()
        {
            var uri = "servers/info";
            return SendGetRequest(uri);
        }

        public Response GetServerStats(string endpoint)
        {
            var uri = $"servers/{endpoint}/stats";
            return SendGetRequest(uri);
        }

        public Response GetPlayerStats(string name)
        {
            var encodedName = HttpUtility.UrlEncode(name);
            var uri = $"players/{encodedName}/stats";
            return SendGetRequest(uri);
        }

        public Response GetRecentMatches(int count)
        {
            var uri = $"reports/recent-matches/{count}";
            return SendGetRequest(uri);
        }

        public Response GetBestPlayers(int count)
        {
            var uri = $"reports/best-players/{count}";
            return SendGetRequest(uri);
        }

        public Response GetPopularServers(int count)
        {
            var uri = $"reports/popular-servers/{count}";
            return SendGetRequest(uri);
        }
    }
}
