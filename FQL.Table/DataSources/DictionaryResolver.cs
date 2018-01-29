using System;
using System.Collections.Generic;
using System.Text;

namespace FQL.Schema.DataSources
{
    public class DictionaryResolver : IObjectResolver
    {
        private readonly IDictionary<string, object> _entity;

        public DictionaryResolver(IDictionary<string, object> entity)
        {
            _entity = entity;
        }

        public object Context => _entity;

        object IObjectResolver.this[string name] => _entity[name];

        object IObjectResolver.this[int index] => null;

        public bool HasColumn(string name)
        {
            return _entity.ContainsKey(name);
        }
    }
}
