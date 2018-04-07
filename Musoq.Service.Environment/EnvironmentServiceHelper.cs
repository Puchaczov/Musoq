using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musoq.Service.Environment
{
    public static class EnvironmentServiceHelper
    {
        public const string PluginsFolderKey = "PluginsFolder";
        public const string HttpServerAddressKey = "HttpServerAddress";
        public const string ServerAddressKey = "ServerAddress";

        private static readonly Plugins.Environment env = new Plugins.Environment();

        public static string PluginsFolder => env.Value<string>(PluginsFolderKey);

        public static string HttpServerAddress => env.Value<string>(HttpServerAddressKey);

        public static string ServerAddress => env.Value<string>(ServerAddressKey);
    }
}
