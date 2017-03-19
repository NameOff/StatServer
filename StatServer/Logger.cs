using log4net;
using log4net.Config;

namespace StatServer
{
    public static class Logger
    {
        public static readonly ILog Log = LogManager.GetLogger("LOGGER");

        public static void InitLogger()
        {
            XmlConfigurator.Configure();
        }
    }
}
