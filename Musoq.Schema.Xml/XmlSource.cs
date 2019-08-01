using Musoq.Schema.DataSources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Musoq.Schema.Xml
{
    public class XmlSource : RowSourceBase<DynamicElement>
    {
        private readonly string _filePath;
        private readonly RuntimeContext _context;

        public XmlSource(string filePath, RuntimeContext context)
        {
            _filePath = filePath;
            _context = context;
        }

        protected override void CollectChunks(System.Collections.Concurrent.BlockingCollection<IReadOnlyList<IObjectResolver>> chunkedSource)
        {
            using (var file = File.OpenRead(_filePath))
            using (var stringReader = new StreamReader(file))
            using (var xmlReader = XmlReader.Create(stringReader, new XmlReaderSettings()
            {
                IgnoreWhitespace = true,
                IgnoreComments = true
            }))
            {
                xmlReader.MoveToContent();

                var chunk = new List<IObjectResolver>(1000);

                var nameToIndexMap = new Dictionary<string, int>();
                var indexToObjectAccessMap = new Dictionary<int, Func<dynamic, object>>();

                var elements = new Stack<DynamicElement>();

                do
                {
                    switch (xmlReader.NodeType)
                    {
                        case XmlNodeType.Element:
                            var element = new DynamicElement
                            {
                                { "element", xmlReader.LocalName },
                                { "parent", elements.Count > 0 ? elements.Peek() : null },
                                { "value", xmlReader.HasValue ? xmlReader.Value : null }
                            };

                            element.Add(xmlReader.Name, element);

                            elements.Push(element);

                            if (xmlReader.HasAttributes)
                            {
                                while (xmlReader.MoveToNextAttribute())
                                {
                                    element.Add(xmlReader.Name, xmlReader.Value);
                                }
                            }

                            xmlReader.MoveToElement();
                            break;
                        case XmlNodeType.Text:
                            elements.Peek().Add("text", xmlReader.Value);
                            break;
                        case XmlNodeType.EndElement:
                            chunk.Add(new DictionaryResolver(elements.Pop()));
                            break;
                    }

                    if (chunk.Count >= 1000)
                    {
                        chunkedSource.Add(chunk);
                        chunk = new List<IObjectResolver>(1000);
                    }
                }
                while (xmlReader.Read());

                if (chunk.Count > 0)
                    chunkedSource.Add(chunk);
            }
        }
    }
}
