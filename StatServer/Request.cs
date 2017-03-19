using System.Net.Http;

namespace StatServer
{
    public class Request
    {
        public readonly HttpMethod Method;
        public readonly string Uri;
        public readonly string Json;

        public Request(HttpMethod method, string uri, string json = null)
        {
            Method = method;
            Uri = uri;
            Json = json;
        }
    }
}
