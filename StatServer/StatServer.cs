﻿using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Http;

namespace StatServer
{
    public class StatServer : IDisposable
    {
        private readonly HttpListener listener;

        private readonly Processor processor;

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
                response = processor.HandleRequest(context.Request.RawUrl, HttpMethod.Put, json);
            }
            else
            {
                response = processor.HandleRequest(context.Request.RawUrl, HttpMethod.Get);
            }

            SendMessage(context, response);
        }

        private static void SendMessage(HttpListenerContext context, HttpResponse response)
        {
            context.Response.StatusCode = response.Code;
            if (response.Message == null)
                return;

            context.Response.ContentLength64 = Encoding.UTF8.GetByteCount(response.Message);
            using (var stream = context.Response.OutputStream)
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(response.Message);
                }
            }
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