using System.Configuration;

namespace Musoq.Service
{
    public static class ApplicationConfiguration
    {
        public static string ServerAddress => ConfigurationManager.AppSettings["ApiAddress"];
        public static string HttpServerAdress => $"http://{ServerAddress}";
        public static string PluginsFolder => "Plugins";
    }
}