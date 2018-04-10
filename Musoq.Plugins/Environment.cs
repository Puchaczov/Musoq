using System;
using System.Collections.Concurrent;

namespace Musoq.Plugins
{
    public class Environment
    {
        [ThreadStatic]
        private static readonly ConcurrentDictionary<string, object> Objects;

        [ThreadStatic]
        private static readonly ConcurrentDictionary<string, Func<object, object>> Converters;

        static Environment()
        {
            Objects = new ConcurrentDictionary<string, object>();
            Converters = new ConcurrentDictionary<string, Func<object, object>>();
        }

        public T Value<T>(string name)
        {
            if (Converters.ContainsKey(name))
                return (T) Converters[name](Objects[name]);

            return (T) Objects[name];
        }

        public void SetValue<T>(string name, T value)
        {
            if (!Objects.TryAdd(name, value))
                Objects.TryUpdate(name, value, Objects[name]);
        }

        public void SetValueWithConverter<T>(string name, T value, Func<object, object> func)
        {
            SetValue(name, value);

            if (!Converters.TryAdd(name, func))
                Converters.TryUpdate(name, func, Converters[name]);
        }
    }
}
