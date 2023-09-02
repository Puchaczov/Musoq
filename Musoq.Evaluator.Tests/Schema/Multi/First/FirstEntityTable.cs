using System;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.Multi.First;

public class FirstEntityTable : ISchemaTable
{
    public ISchemaColumn[] Columns => Array.Empty<ISchemaColumn>();
    
    public ISchemaColumn GetColumnByName(string name)
    {
        return Columns[FirstEntity.TestNameToIndexMap[name]];
    }

    public SchemaTableMetadata Metadata { get; } = new(typeof(FirstEntity));
}