using System;
using Musoq.Evaluator.Tests.Schema.Multi.First;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.Multi.Second;

public class SecondEntityTable : ISchemaTable
{
    public ISchemaColumn[] Columns => Array.Empty<ISchemaColumn>();
    
    public ISchemaColumn GetColumnByName(string name)
    {
        return Columns[FirstEntity.TestNameToIndexMap[name]];
    }

    public SchemaTableMetadata Metadata { get; } = new(typeof(SecondEntity));
}