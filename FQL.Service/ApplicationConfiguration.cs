using System.Configuration;

namespace FQL.Service
{
    public static class ApplicationConfiguration
    {
        public static string ServerAddress => ConfigurationManager.AppSettings["ApiAddress"];
        public static string PluginsFolder => "Plugins";

        public static string SchemasFolder => "Schemas";
    }
}