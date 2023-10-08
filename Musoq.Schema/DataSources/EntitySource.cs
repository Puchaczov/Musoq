using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Schema.DataSources
{
    public class EntitySource<T> : RowSource
    {
        private readonly IEnumerable<T> _entities;
        private readonly IDictionary<int, Func<T, object>> _indexToObjectAccessMap;
        private readonly IDictionary<string, int> _nameToIndexMap;

        public EntitySource(IEnumerable<T> entities, IDictionary<string, int> nameToIndexMap,
            IDictionary<int, Func<T, object>> indexToObjectAccessMap)
        {
            _entities = entities;
            _nameToIndexMap = nameToIndexMap;
            _indexToObjectAccessMap = indexToObjectAccessMap;
        }

        public override IEnumerable<IObjectResolver> Rows
        {
            get
            {
                return _entities.Select(item => new EntityResolver<T>(item, _nameToIndexMap, _indexToObjectAccessMap));
            }
        }
    }
}