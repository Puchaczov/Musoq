using System.Collections.Generic;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tables
{
    public class RowResolver(ObjectsRow row, IDictionary<string, int> nameToIndexMap) : IObjectResolver
    {
        public object[] Contexts => row.Contexts;

        public bool HasColumn(string name)
        {
            return nameToIndexMap.ContainsKey(name);
        }

        object IObjectResolver.this[string name]
        {
            get
            {
                if (!nameToIndexMap.TryGetValue(name, out var value))
                    throw new System.Exception($"Column with name {name} does not exist in the row.");
                
                return row[value];
            }
        }

        object IObjectResolver.this[int index] => row[index];
    }
}