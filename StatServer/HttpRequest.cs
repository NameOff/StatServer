using System.Net.Http;

namespace StatServer
{
    public class HttpRequest
    {
        public readonly HttpMethod Method;
        public readonly string Uri;
        public readonly string Json;

        public HttpRequest(HttpMethod method, string uri, string json = null)
        {
            Method = method;
            Uri = uri;
            Json = json;
        }
    }
}
