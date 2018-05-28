using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Musoq.Service.Client.Core;
using Musoq.Service.Client.Core.Helpers;

namespace Musoq.Service.Core.Controllers
{
    public class SelfController : ControllerBase
    {
        private const string Query = "select FullName, Sha256File() from #disk.directory('{0}', 'true')";

        private static readonly Assembly EntryAssembly = Assembly.GetEntryAssembly();
        private static readonly string EntryDir = Path.GetDirectoryName(EntryAssembly.Location);
        private static readonly string PluginsDir = Path.Combine(EntryDir, ApplicationConfiguration.PluginsFolder);

        [HttpPost]
        public Guid UsedFiles()
        {
            var context = new QueryContext
            {
                Query = string.Format(Query, EntryDir)
            };

            var api = new ContextApi(ApplicationConfiguration.HttpServerAdress);

            var result = api.Create(context).Result;
            return result;
        }

        [HttpGet]
        public IEnumerable<string> Plugins()
        {
            return Directory.GetDirectories(PluginsDir);
        }
    }
}