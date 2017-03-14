using System;
using System.Data;
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

        private bool isListening;

        public Database Database;
        public Cache Cache;

        public StatServer()
        {
            listener = new HttpListener();
            Database = new Database();
            Cache = new Cache(Database);
            processor = new Processor(Database, Cache);
            Database.Connection.Close();
        }

        public void ClearDatabaseAndCache()
        {
            if (Database.Connection.State == ConnectionState.Closed)
                Database.Connection.Open();
            Database.DropAllTables();
            Database.CreateAllTables();
            Cache = new Cache(Database);
            Database.Connection.Close();
        }

        public void Stop()
        {
            isListening = false;
            listener.Stop();
            listener.Close();
            Database.Connection.Close();
        }

        public void Start(string prefix)
        {
            listener.Prefixes.Add(prefix);
            listener.Start();
            Database.Connection.Open();
            isListening = true;
            while (isListening)
            {
                try
                {
                    var context = listener.GetContext();
                    HandleRequest(context);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (HttpListenerException)
                {
                    break;
                }
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
            ((IDisposable)listener)?.Dispose();
        }
    }
}
