using System.Collections.Generic;
using System.IO;
using System.Linq;
using Musoq.Schema.DataSources;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Musoq.Schema.Json
{
    public class JsonSource : RowSource
    {
        private readonly string _filePath;

        public JsonSource(string filePath)
        {
            _filePath = filePath;
        }

        public override IEnumerable<IObjectResolver> Rows
        {
            get
            {
                using (JsonReader reader = new JsonTextReader(new StreamReader(File.OpenRead(_filePath))))
                {
                    reader.SupportMultipleContent = true;

                    var serializer = new JsonSerializer();
                    while (reader.Read())
                    {
                        if (reader.TokenType != JsonToken.StartObject) continue;

                        var obj = (JObject)serializer.Deserialize(reader);
                        var props = new Stack<JProperty>();

                        foreach (var prop in obj.Properties().Reverse())
                            props.Push(prop);

                        var row = new Dictionary<string, object>();

                        while (props.Count > 0)
                        {
                            var prop = props.Pop();

                            switch (prop.Value.Type)
                            {
                                case JTokenType.None:
                                    break;
                                case JTokenType.Object:
                                    foreach (var mprop in ((JObject)prop.Value).Properties().Reverse())
                                        props.Push(mprop);
                                    break;
                                case JTokenType.Array:
                                    row.Add(prop.Name, (JArray)prop.Value);
                                    break;
                                case JTokenType.Constructor:
                                    break;
                                case JTokenType.Property:
                                    break;
                                case JTokenType.Comment:
                                    break;
                                case JTokenType.Integer:
                                    row.Add(prop.Name, JsonBasedTable.GetValue(prop.Value));
                                    break;
                                case JTokenType.Float:
                                    row.Add(prop.Name, JsonBasedTable.GetValue(prop.Value));
                                    break;
                                case JTokenType.String:
                                    row.Add(prop.Name, JsonBasedTable.GetValue(prop.Value));
                                    break;
                                case JTokenType.Boolean:
                                    row.Add(prop.Name, JsonBasedTable.GetValue(prop.Value));
                                    break;
                                case JTokenType.Null:
                                    break;
                                case JTokenType.Undefined:
                                    break;
                                case JTokenType.Date:
                                    break;
                                case JTokenType.Raw:
                                    break;
                                case JTokenType.Bytes:
                                    break;
                                case JTokenType.Guid:
                                    break;
                                case JTokenType.Uri:
                                    break;
                                case JTokenType.TimeSpan:
                                    break;
                            }
                        }

                        yield return new DictionaryResolver(row);
                    }
                }
            }
        }
    }
}
