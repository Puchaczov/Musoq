using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Musoq.Schema.DataSources;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Musoq.Schema.Json
{
    public class JsonBasedTable : ISchemaTable
    {
        private readonly string _path;
        private readonly string _select;

        public JsonBasedTable(string filePath, string select)
        {
            _path = filePath;
            _select = select;
        }

        public ISchemaColumn[] Columns
        {
            get
            {
                var file = File.ReadAllText(_path);
                var schema = JsonConvert.DeserializeObject(file);

                switch (schema)
                {
                    case JObject jobj:
                        return ParseObject(jobj);
                    case JArray jarr:
                        return ParseArray(jarr);
                }

                throw new NotSupportedException();
            }
        }

        private ISchemaColumn[] ParseArray(JArray jarr)
        {
            return new ISchemaColumn[]
            {
                new SchemaColumn("Array", 0, typeof(JArray))
            };
        }

        private ISchemaColumn[] ParseObject(JObject jobj)
        {
            var obj = jobj;

            var props = new Stack<JProperty>();

            foreach (var prop in obj.Properties().Reverse())
                props.Push(prop);
            
            var columns = new List<ISchemaColumn>();
            var columnIndex = 0;

            while (props.Count > 0)
            {
                var prop = props.Pop();

                switch (prop.Value.Type)
                {
                    case JTokenType.None:
                        break;
                    case JTokenType.Object:
                        columns.Add(new SchemaColumn(prop.Name, columnIndex++, typeof(JObject)));
                        foreach (var mprop in ((JObject) prop.Value).Properties().Reverse())
                            props.Push(mprop);
                        break;
                    case JTokenType.Array:
                        columns.Add(new SchemaColumn(prop.Name, columnIndex++, typeof(JArray)));
                        break;
                    case JTokenType.Constructor:
                        break;
                    case JTokenType.Property:
                        break;
                    case JTokenType.Comment:
                        break;
                    case JTokenType.Integer:
                        columns.Add(new SchemaColumn(prop.Name, columnIndex++, GetType(prop.Value)));
                        break;
                    case JTokenType.Float:
                        columns.Add(new SchemaColumn(prop.Name, columnIndex++, GetType(prop.Value)));
                        break;
                    case JTokenType.String:
                        columns.Add(new SchemaColumn(prop.Name, columnIndex++, GetType(prop.Value)));
                        break;
                    case JTokenType.Boolean:
                        columns.Add(new SchemaColumn(prop.Name, columnIndex++, GetType(prop.Value)));
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

            return columns.ToArray();
        }

        public static Type GetType(JToken value)
        {
            switch (value.Value<string>().ToLowerInvariant())
            {
                case "float":
                    return typeof(decimal);
                case "int":
                    return typeof(int);
                case "string":
                    return typeof(string);
                case "bool":
                case "boolean":
                    return typeof(bool);
            }

            throw new NotSupportedException($"Type {value.Value<string>()} is not supported.");
        }

        public static object GetValue(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Float:
                    return token.Value<decimal>();
                case JTokenType.Integer:
                    return token.Value<int>();
                case JTokenType.String:
                    return token.Value<string>();
                case JTokenType.Boolean:
                    return token.Value<bool>();
            }

            throw new NotSupportedException($"Type {token.Value<string>()} is not supported.");
        }
    }
}