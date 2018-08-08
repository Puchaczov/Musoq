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
        private readonly T _entitiy;
        private readonly IDictionary<int, Func<T, object>> _indexToObjectAccessMap;
        private readonly IDictionary<string, int> _nameToIndexMap;

        public EntityResolver(T entity, IDictionary<string, int> nameToIndexMap,
            IDictionary<int, Func<T, object>> indexToObjectAccessMap)
        {
            _entitiy = entity;
            _nameToIndexMap = nameToIndexMap;
            _indexToObjectAccessMap = indexToObjectAccessMap;
        }

        public object Context => _entitiy;

        object IObjectResolver.this[string name]
            => _indexToObjectAccessMap[_nameToIndexMap[name]](_entitiy);

        object IObjectResolver.this[int index]
            => _indexToObjectAccessMap[index](_entitiy);

        public bool HasColumn(string name)
        {
            return _nameToIndexMap.ContainsKey(name);
        }

#if DEBUG
        public string DebugString()
        {
            return $"{_entitiy.ToString()}";
        }
#endif
    }
}