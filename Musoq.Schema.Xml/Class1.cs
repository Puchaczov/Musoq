using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Xml
{
    public class XmlSource : RowSourceBase<XElement>
    {
        private readonly InterCommunicator _interCommunicator;
        private readonly string _xpath;
        private readonly string[] _filePaths;

        public XmlSource(string filePath, string xpath, InterCommunicator interCommunicator)
        {
            _filePaths = new[] { filePath };
            _xpath = xpath;
            _interCommunicator = interCommunicator;
        }

        protected override void CollectChunks(BlockingCollection<IReadOnlyList<EntityResolver<XElement>>> chunkedSource)
        {
            var tasks = new Task[_filePaths.Length];
            for (var index = 0; index < _filePaths.Length; index++)
            {
                var filePath = _filePaths[index];
                tasks[index] = ParseXml(filePath, chunkedSource);
            }

            Task.WaitAll(tasks);
        }

        private async Task ParseXml(string filePath, BlockingCollection<IReadOnlyList<EntityResolver<XElement>>> chunkedSource)
        {
            if (!File.Exists(filePath))
                return;

            using(var stream = File.OpenRead(filePath))
            {
                var element = await XElement.LoadAsync(stream, LoadOptions.None, _interCommunicator.EndWorkToken);
                chunkedSource.Add(new List<EntityResolver<XElement>> { new EntityResolver<XElement>(element, null, null) });
            }
        }
    }
}
