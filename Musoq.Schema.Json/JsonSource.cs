using System;
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
        private readonly InterCommunicator _communicator;

        public JsonSource(string filePath, InterCommunicator communicator)
        {
            _filePath = filePath;
            _communicator = communicator;
        }

        public override IEnumerable<IObjectResolver> Rows
        {
            get
            {
                var endWorkToken = _communicator.EndWorkToken;
                using (var file = File.OpenRead(_filePath))
                {
                    using (JsonReader reader = new JsonTextReader(new StreamReader(file)))
                    {
                        reader.SupportMultipleContent = true;

                        var serializer = new JsonSerializer();

                        if (!reader.Read())
                            throw new NotSupportedException("Cannot read file. Json is probably malformed.");

                        reader.Read();

                        IEnumerable<IObjectResolver> rows = null;
                        if (reader.TokenType == JsonToken.StartObject)
                            rows = ParseObject(serializer, reader);
                        else if (reader.TokenType == JsonToken.StartArray)
                            rows = ParseArray(serializer, reader);

                        if (rows == null)
                            throw new NotSupportedException("This type of .json file is not supported.");

                        endWorkToken.ThrowIfCancellationRequested();

                        foreach (var row in rows)
                            yield return row;
                    }
                }
            }
        }

        private IEnumerable<IObjectResolver> ParseArray(JsonSerializer serializer, JsonReader reader)
        {
            while (reader.TokenType == JsonToken.StartArray)
            {
                var dict = new Dictionary<string, object>();
                var intArr = (JArray) serializer.Deserialize(reader);
                dict.Add("Array", intArr);
                reader.Read();
                yield return new DictionaryResolver(dict);
            }
        }

        private IEnumerable<IObjectResolver> ParseObject(JsonSerializer serializer, JsonReader reader)
        {
            while (reader.TokenType == JsonToken.StartObject)
            {
                var obj = (JObject) serializer.Deserialize(reader);
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
                            foreach (var mprop in ((JObject) prop.Value).Properties().Reverse())
                                props.Push(mprop);
                            break;
                        case JTokenType.Array:
                            row.Add(prop.Name, (JArray) prop.Value);
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

                reader.Read();
                yield return new DictionaryResolver(row);
            }
        }
    }
}