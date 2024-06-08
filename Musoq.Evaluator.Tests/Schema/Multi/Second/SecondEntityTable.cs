using Musoq.Evaluator.Tests.Schema.Multi.First;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.Multi.Second;

public class SecondEntityTable : ISchemaTable
{
    public ISchemaColumn[] Columns => [
        new SchemaColumn(nameof(SecondEntity.ZeroItem), 0, typeof(string)),
        new SchemaColumn(nameof(SecondEntity.FirstItem), 1, typeof(string))
    ];
    
    public ISchemaColumn GetColumnByName(string name)
    {
        return Columns[FirstEntity.TestNameToIndexMap[name]];
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return
        [
            Columns[FirstEntity.TestNameToIndexMap[name]]
        ];
    }

    public SchemaTableMetadata Metadata { get; } = new(typeof(SecondEntity));
}