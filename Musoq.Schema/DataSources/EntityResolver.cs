using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Musoq.Schema.DataSources
{
#if DEBUG
    [DebuggerDisplay("{" + nameof(DebugString) + "()}")]
#endif
    public class EntityResolver<T> : IObjectResolver
    {
        private readonly T _entity;
        private readonly IDictionary<int, Func<T, object>> _indexToObjectAccessMap;
        private readonly IDictionary<string, int> _nameToIndexMap;

        public EntityResolver(T entity, IDictionary<string, int> nameToIndexMap,
            IDictionary<int, Func<T, object>> indexToObjectAccessMap)
        {
            _entity = entity;
            _nameToIndexMap = nameToIndexMap;
            _indexToObjectAccessMap = indexToObjectAccessMap;
        }

        public object[] Contexts => new object[] { _entity };

        object IObjectResolver.this[string name]
            => _indexToObjectAccessMap[_nameToIndexMap[name]](_entity);

        object IObjectResolver.this[int index]
            => _indexToObjectAccessMap[index](_entity);

        public bool HasColumn(string name)
        {
            return _nameToIndexMap.ContainsKey(name);
        }

#if DEBUG
        public string DebugString()
        {
            return $"{_entity.ToString()}";
        }
#endif
    }
}