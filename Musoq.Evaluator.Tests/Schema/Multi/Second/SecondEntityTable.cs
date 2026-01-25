using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.Multi.Second;

public class SecondEntityTable : ISchemaTable
{
    public ISchemaColumn[] Columns =>
    [
        new SchemaColumn(nameof(SecondEntity.ZeroItem), 0, typeof(string)),
        new SchemaColumn(nameof(SecondEntity.FirstItem), 1, typeof(string))
    ];

    public ISchemaColumn GetColumnByName(string name)
    {
        return Columns[SecondEntity.TestNameToIndexMap[name]];
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        if (SecondEntity.TestNameToIndexMap.TryGetValue(name, out var index))
            return
            [
                Columns[index]
            ];

        return [];
    }

    public SchemaTableMetadata Metadata { get; } = new(typeof(SecondEntity));
}
