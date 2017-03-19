using System.Net;
using System.Net.Http;

namespace StatServer
{
    public class Request
    {
        public readonly HttpMethod Method;
        public readonly string Uri;
        public readonly string Json;
        public readonly EndPoint ClientEndPoint;

        public Request(HttpMethod method, string uri, EndPoint clientEndPoint, string json = null)
        {
            Method = method;
            Uri = uri;
            Json = json;
            ClientEndPoint = clientEndPoint;
        }
    }
}
