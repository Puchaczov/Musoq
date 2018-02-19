using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Plugins
{
    public class Environment
    {
        private readonly ConcurrentDictionary<string, object> _objects;

        public Environment()
        {
            _objects = new ConcurrentDictionary<string, object>();
        }

        public T Value<T>(string name)
        {
            return (T) _objects[name];
        }
    }
}
