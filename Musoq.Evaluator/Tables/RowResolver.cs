using System.Collections.Generic;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tables
{
    public class RowResolver : IObjectResolver
    {
        private readonly IDictionary<string, int> _nameToIndexMap;
        private readonly ObjectsRow _row;

        public RowResolver(ObjectsRow row, IDictionary<string, int> nameToIndexMap)
        {
            _row = row;
            _nameToIndexMap = nameToIndexMap;
        }

        public object[] Contexts => _row.Contexts;

        public bool HasColumn(string name)
        {
            return _nameToIndexMap.ContainsKey(name);
        }

        object IObjectResolver.this[string name]
        {
            get
            {
#if DEBUG
                if (!_nameToIndexMap.TryGetValue(name, out var value))
                    throw new System.Exception($"Column with name {name} does not exist in the row.");
                
                return _row[value];
#else
                return _row[_nameToIndexMap[name]];
#endif
            }
        }

        object IObjectResolver.this[int index] => _row[index];
    }
}