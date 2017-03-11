using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Http;
using static System.String;

namespace StatServer
{
    public class StatServer : IDisposable
    {
        private readonly HttpListener listener;

        public readonly Processor processor;

        public const int ThreadsCount = 10;

        public StatServer()
        {
            listener = new HttpListener();
            processor = new Processor();
        }

        public void Start(string prefix)
        {
            listener.Prefixes.Add(prefix);
            listener.Start();
            while (true)
            {
                var context = listener.GetContext();
                HandleRequest(context);
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            HttpResponse response;

            if (context.Request.HttpMethod == HttpMethod.Put.ToString())
            {
                var json = GetRequestPostJson(context.Request);
                response = processor.HandleRequest(new HttpRequest(HttpMethod.Put, context.Request.RawUrl, json));
            }
            else
            {
                response = processor.HandleRequest(new HttpRequest(HttpMethod.Get, context.Request.RawUrl));
            }

            Console.WriteLine(context.Request.UserHostName);

            SendMessage(context, response);
        }

        private static void SendMessage(HttpListenerContext context, HttpResponse response)
        {
            context.Response.StatusCode = response.Code;

            if (response.Message != null)
            {
                context.Response.ContentLength64 = Encoding.UTF8.GetByteCount(response.Message);
                using (var stream = context.Response.OutputStream)
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(response.Message);
                    }
                }
            }

            RemoveHeaders(context.Response);
            context.Response.Close();
        }

        private static void RemoveHeaders(HttpListenerResponse response)
        {
            response.Headers.Add("Server", Empty);
            response.Headers.Add("Date", Empty);
            response.KeepAlive = false;
        }

        private static string GetRequestPostJson(HttpListenerRequest request)
        {
            if (!request.HasEntityBody)
                return null;

            using (var body = request.InputStream)
            {
                using (var reader = new StreamReader(body, request.ContentEncoding))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public void Dispose()
        {
            ((IDisposable) listener)?.Dispose();
        }
    }
}
