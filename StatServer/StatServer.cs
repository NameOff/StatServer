using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

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
            Logger.InitLogger();
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
            lock (listener)
            {
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
                listenerThread.Join();
            }
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
                        Task.Run(() => HandleRequestAsync(context));
                    }
                }
                catch (ObjectDisposedException)
                {
                    Logger.Log.Debug("Server disposed");
                }
                catch (HttpListenerException)
                {
                    Logger.Log.Debug("Connection lost");
                }
                catch (Exception e)
                {
                    Logger.Log.Debug($"Unexcpected exception: {e.Message}");
                }
            }
        }

        private async void HandleRequestAsync(HttpListenerContext context)
        {
            try
            {
                Response response;
                if (context.Request.HttpMethod == HttpMethod.Put.ToString())
                {
                    var json = GetRequestPostJson(context.Request);
                    response = processor.HandleRequest(new Request(HttpMethod.Put, context.Request.RawUrl, context.Request.RemoteEndPoint, json));
                }
                else
                {
                    response = processor.HandleRequest(new Request(HttpMethod.Get, context.Request.RawUrl, context.Request.RemoteEndPoint));
                }
                if (response.Code == (int) Response.Status.MethodNotAllowed)
                    Logger.Log.Error($"Method not allowed. Client: {context.Request.RemoteEndPoint}");
                await SendMessage(context, response);
            }
            catch (HttpListenerException)
            {

            }
        }

        private static async Task SendMessage(HttpListenerContext context, Response response)
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

            context.Response.Close();
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
