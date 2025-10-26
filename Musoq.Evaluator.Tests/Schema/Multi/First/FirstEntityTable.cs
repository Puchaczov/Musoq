using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.Multi.First;

public class FirstEntityTable : ISchemaTable
{
    public ISchemaColumn[] Columns => [
        new SchemaColumn(nameof(FirstEntity.FirstItem), 0, typeof(string))
    ];
    
    public ISchemaColumn GetColumnByName(string name)
    {
        return Columns[FirstEntity.TestNameToIndexMap[name]];
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        if (FirstEntity.TestNameToIndexMap.TryGetValue(name, out var index))
        {
            return [
                Columns[index]
            ];
        
        }
        
        return [];
    }

    public SchemaTableMetadata Metadata { get; } = new(typeof(FirstEntity));
}
