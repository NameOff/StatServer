using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static System.String;

namespace StatServer
{
    public class StatServer : IDisposable
    {
        private readonly HttpListener listener;

        public readonly Processor processor;

        public const int MaxThreadsCount = 300;

        private Thread listenerThread;

        private bool isRunning;

        public Database Database;
        public Cache Cache;

        public const int ReportStatsMaxCount = 50;
        public const int ReportStatsMinCount = 0;
        public const int ReportStatsDefaultCount = 5;

        public StatServer()
        {
            ServicePointManager.DefaultConnectionLimit = MaxThreadsCount;
            ThreadPool.SetMinThreads(MaxThreadsCount, MaxThreadsCount);
            ThreadPool.SetMaxThreads(MaxThreadsCount, MaxThreadsCount);
            listener = new HttpListener();
            processor = new Processor();
        }

        public void ClearDatabaseAndCache()
        {
            processor.ClearDatabaseAndCache();
        }

        public void Stop()
        {
            isRunning = false;
            listener.Stop();
            listener.Close();
        }

        public void Start(string prefix)
        {
            if (isRunning)
                return;
            
            listener.Prefixes.Clear();
            listener.Prefixes.Add(prefix);
            listener.Start();
            listenerThread = new Thread(Listen)
            {
                IsBackground = true,
                Priority = ThreadPriority.Highest
            };
            listenerThread.Start();

            isRunning = true;
            Console.ReadKey();
        }

        private void Listen()
        {
            while (true)
            {
                try
                {
                    if (listener.IsListening)
                    {
                        var context = listener.GetContext();
                        //HandleRequestAsync(context, counter);
                        Task.Run(() => HandleRequestAsync(context));
                    }
                }
                catch (ObjectDisposedException)
                {
                    //TODO Log
                }
                catch (HttpListenerException)
                {
                    //TODO Log
                }
            }
        }

        private async void HandleRequestAsync(HttpListenerContext context)
        {
            try
            {
                var timer = new Stopwatch();
                //Console.WriteLine($"Время начала {DateTime.Now}");
                timer.Start();
                //Thread.Sleep(1000);
                
                
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
                
                timer.Stop();
                //Console.WriteLine($"Затраченное время: {timer.Elapsed}");
                //Console.WriteLine(context.Request.UserHostName);
                //Console.WriteLine(response.Code);
                //Console.WriteLine(processor.RequestsCount);
                //var response = new HttpResponse(HttpResponse.Status.OK);
                
                await SendMessage(context, response);
            }
            catch (HttpListenerException)
            {

            }
        }

        private static async Task SendMessage(HttpListenerContext context, HttpResponse response)
        {
            context.Response.StatusCode = response.Code;

            if (response.Message != null)
            {
                context.Response.ContentLength64 = Encoding.UTF8.GetByteCount(response.Message);
                using (var stream = context.Response.OutputStream)
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        await writer.WriteAsync(response.Message);
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
