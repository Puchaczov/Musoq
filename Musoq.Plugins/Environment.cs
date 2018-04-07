using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Plugins
{
    public class Environment
    {
        private static readonly ConcurrentDictionary<string, object> Objects;

        static Environment()
        {
            Objects = new ConcurrentDictionary<string, object>();
        }

        public T Value<T>(string name)
        {
            return (T) Objects[name];
        }

        public void SetValue<T>(string name, T value)
        {
            if (!Objects.TryAdd(name, value))
                Objects.TryUpdate(name, value, Objects[name]);
        }
    }
}
