using System.Collections.Generic;
using Musoq.Parser.Nodes;
using Musoq.Schema;
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

    private static SchemaColumn CreateLogicalEnumSchemaColumn(
        CreateTableColumnDefinition column,
        int index,
        Type carrierType,
        EnumTypeDescriptor descriptor)
    {
        IReadOnlyDictionary<string, string>? modifiers = null;
        if (column.ReadModifiers.Count > 0)
        {
            var modifierCopy = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var modifier in column.ReadModifiers)
                modifierCopy.Add(modifier.Key, modifier.Value);
            modifiers = modifierCopy;
        }

        return new SchemaColumn(
            column.ColumnName,
            index,
            carrierType,
            carrierType,
            descriptor,
            intendedTypeName: null,
            readModifiers: modifiers,
            stability: ColumnStability.Stable);
    }
}
