namespace Musoq.Service.Environment
{
    public static class EnvironmentServiceHelper
    {
        public const string PluginsFolderKey = "PluginsFolder";
        public const string HttpServerAddressKey = "HttpServerAddress";
        public const string ServerAddressKey = "ServerAddress";
        public const string TempFolderKey = "TempFolder";

        private static readonly Plugins.Environment Env = new Plugins.Environment();

        public static string PluginsFolder => Env.Value<string>(PluginsFolderKey);

        public static string HttpServerAddress => Env.Value<string>(HttpServerAddressKey);

        public static string ServerAddress => Env.Value<string>(ServerAddressKey);

        public static string TempFolder => Env.Value<string>(TempFolderKey);
    }
}