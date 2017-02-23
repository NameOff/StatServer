using System;
using System.Net.Configuration;
using System.Text.RegularExpressions;
using Fclp;
using System.Web;

namespace StatServer
{
    public class EntryPoint
    {
        public static void Main(string[] args)
        {
            var commandLineParser = new FluentCommandLineParser<Options>();

            commandLineParser
                .Setup(options => options.Prefix)
                .As("prefix")
                .SetDefault("http://+:8080/")
                .WithDescription("HTTP prefix to listen on");

            commandLineParser
                .SetupHelp("h", "help")
                .WithHeader($"{AppDomain.CurrentDomain.FriendlyName} [--prefix <prefix>]")
                .Callback(text => Console.WriteLine(text));

            if (commandLineParser.Parse(args).HelpCalled)
                return;

            RunServer(commandLineParser.Object);
        }

        private static void RunServer(Options options)
        {
            

            using (var server = new StatServer())
                server.Start(options.Prefix);
        }

        private class Options
        {
            public string Prefix { get; set; }
        }
    }
}
