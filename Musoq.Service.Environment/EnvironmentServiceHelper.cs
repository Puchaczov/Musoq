namespace Musoq.Service.Environment
{
    public static class EnvironmentServiceHelper
    {
        public const string PluginsFolderKey = "PluginsFolder";
        public const string HttpServerAddressKey = "HttpServerAddress";
        public const string ServerAddressKey = "ServerAddress";
        public const string TempFolderKey = "TempFolder";

        private static readonly Plugins.Environment env = new Plugins.Environment();

        public static string PluginsFolder => env.Value<string>(PluginsFolderKey);

        public static string HttpServerAddress => env.Value<string>(HttpServerAddressKey);

        public static string ServerAddress => env.Value<string>(ServerAddressKey);

        public static string TempFolder => env.Value<string>(TempFolderKey);
    }
}