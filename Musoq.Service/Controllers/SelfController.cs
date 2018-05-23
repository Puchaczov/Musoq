using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;
using Musoq.Service.Client;
using Musoq.Service.Client.Helpers;

namespace Musoq.Service.Controllers
{
    public class SelfController : ApiController
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

        [HttpPost]
        public HttpResponseMessage UploadPlugin()
        {
            if (!Request.Content.IsMimeMultipartContent())
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);

            try
            {
                var provider = new MultipartFormDataStreamProvider(Path.GetTempPath());

                foreach (var file in provider.FileData)
                {
                    var pluginSpecificDir = Path.Combine(PluginsDir, file.Headers.ContentDisposition.FileName);

                    if (!Directory.Exists(pluginSpecificDir))
                        Directory.CreateDirectory(pluginSpecificDir);

                    ZipFile.ExtractToDirectory(file.LocalFileName, pluginSpecificDir);
                }

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception exc)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }
    }
}