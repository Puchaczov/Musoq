using System.Configuration;

namespace Musoq.Console.Helpers
{
    public static class Configuration
    {
        public static string Address => ConfigurationManager.AppSettings["ApiAddress"];
    }
}