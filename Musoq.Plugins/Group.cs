using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Musoq.Plugins
{
#if DEBUG
    [DebuggerDisplay("{Name}")]
#endif
    public class Group
    {
        public Group(Group parent, string[] fieldNames, object[] values)
        {
            Parent = parent;
#if DEBUG
            Name = fieldNames.Length == 0 ? "root" : fieldNames.Aggregate((a, b) => a + ',' + b);
#endif
            for (var i = 0; i < fieldNames.Length; i++) Values.Add(fieldNames[i], values[i]);
        }

#if DEBUG
        private string Name { get; }
#endif

        public Group Parent { get; }
        public int Count { get; private set; }

        private IDictionary<string, object> Values { get; } = new Dictionary<string, object>();

        private IDictionary<string, Func<object, object>> Converters { get; } =
            new Dictionary<string, Func<object, object>>();

        public void Hit()
        {
            Count += 1;
        }

        public T GetValue<T>(string name)
        {
            if (!Values.ContainsKey(name))
                throw new KeyNotFoundException($"Group does not have value {name}.");

            if (Converters.ContainsKey(name))
                return (T) Converters[name](Values[name]);

            return (T) Values[name];
        }

        public T GetRawValue<T>(string name)
        {
            if (!Values.ContainsKey(name))
                throw new KeyNotFoundException($"Group does not have value {name}.");

            return (T) Values[name];
        }

        public T GetOrCreateValue<T>(string name, T defValue = default)
        {
            if (!Values.ContainsKey(name))
                Values.Add(name, defValue);

            return (T) Values[name];
        }

        public T GetOrCreateValue<T>(string name, Func<T> createDefault)
        {
            if (!Values.ContainsKey(name))
                Values.Add(name, createDefault());

            return (T)Values[name];
        }

        public void SetValue<T>(string name, T value)
        {
            Values[name] = value;
        }

        public TR GetOrCreateValueWithConverter<T, TR>(string name, T value, Func<object, object> converter)
        {
            if (!Values.ContainsKey(name))
                Values.Add(name, value);

            if (!Converters.ContainsKey(name))
                Converters.Add(name, converter);

            return (TR) Converters[name](Values[name]);
        }
    }
}