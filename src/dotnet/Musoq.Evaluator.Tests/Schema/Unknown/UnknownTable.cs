using System.Collections.Generic;
using System.Linq;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.Unknown;

public class UnknownTable(SourceMetadataContext runtimeContext) : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } = runtimeContext.AllColumns.ToArray();

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(col => col.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(col => col.ColumnName == name).ToArray();
    }

    public SchemaTableMetadata Metadata { get; } = new(typeof(IReadOnlyDictionary<string, object>));
}
