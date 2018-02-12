using System;
using System.IO;
using System.Reflection;
using System.Web.Http;
using Musoq.Service.Client;
using Musoq.Service.Client.Helpers;

namespace Musoq.Service.Controllers
{
    public class SelfController : ApiController
    {
        private const string Query = "select FullName, Sha256File() from #disk.directory('{0}', 'true')";

        [HttpPost]
        public Guid UsedFiles()
        {
            var assembly = Assembly.GetEntryAssembly();
            var context = new QueryContext()
            {
                Query = string.Format(Query, Path.GetDirectoryName(assembly.Location))
            };

            var api = new ContextApi($"http://{ApplicationConfiguration.ServerAddress}");

            var result = api.Create(context).Result;
            return result;
        }
    }
}
