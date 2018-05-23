using System;
using System.Dynamic;
using Newtonsoft.Json.Linq;

namespace Musoq.Schema.Json
{
    public class DynamicJsonWrapper : DynamicObject
    {
        private readonly JObject _obj;

        public DynamicJsonWrapper(JObject obj)
        {
            _obj = obj;
        }

        public object this[string name]
        {
            get
            {
                var value = _obj.GetValue(name);
                switch (value.Type)
                {
                    case JTokenType.None:
                        break;
                    case JTokenType.Object:
                        return new DynamicJsonWrapper((JObject) value);
                    case JTokenType.Array:
                        break;
                    case JTokenType.Constructor:
                        break;
                    case JTokenType.Property:
                        break;
                    case JTokenType.Comment:
                        break;
                    case JTokenType.Integer:
                        return value.ToObject<int>();
                    case JTokenType.Float:
                        return value.ToObject<decimal>();
                    case JTokenType.String:
                        return value.ToObject<string>();
                    case JTokenType.Boolean:
                        return value.ToObject<bool>();
                    case JTokenType.Null:
                        break;
                    case JTokenType.Undefined:
                        break;
                    case JTokenType.Date:
                        return value.ToObject<DateTimeOffset>();
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
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                throw new NotSupportedException();
            }
        }
    }
}