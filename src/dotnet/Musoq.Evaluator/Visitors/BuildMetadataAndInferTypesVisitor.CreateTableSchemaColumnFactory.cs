using System.Collections.Generic;
using Musoq.Parser.Nodes;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private static SchemaColumn CreateSchemaColumn(CreateTableColumnDefinition column, int index, Type type)
    {
        if (column.ReadModifiers.Count == 0)
            return new SchemaColumn(column.ColumnName, index, type);

        var modifiers = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var modifier in column.ReadModifiers)
            modifiers.Add(modifier.Key, modifier.Value);

        return new SchemaColumn(column.ColumnName, index, type, modifiers);
    }
}
