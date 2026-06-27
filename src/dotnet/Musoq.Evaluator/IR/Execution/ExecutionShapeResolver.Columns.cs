using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionShapeResolver
{
    private ISchemaColumn[] ResolveColumns(PhysicalSchemaScanNode scan)
    {
        ISchemaColumn[] columns;
        if (_inferredColumns.TryGetValue(scan.Alias, out var inferredColumns) && inferredColumns.Length > 0)
        {
            columns = inferredColumns;
        }
        else
        {
            columns = scan.OutputSchema.Columns
                .Select(column => (ISchemaColumn)new SchemaColumn(column.Name, column.Index, column.Type))
                .ToArray();
        }

        return FilterProjectedColumns(columns, scan.ProjectedColumns);
    }

    private static ISchemaColumn[] FilterProjectedColumns(
        ISchemaColumn[] columns,
        string[] projectedColumns)
    {
        if (projectedColumns.Length == 0)
            return columns;

        var projectedColumnNames = new HashSet<string>(projectedColumns, StringComparer.OrdinalIgnoreCase);
        var filteredColumns = columns
            .Where(column => projectedColumnNames.Contains(column.ColumnName))
            .ToArray();

        return filteredColumns.Length == projectedColumns.Length
            ? filteredColumns
            : columns;
    }

    private static ISchemaColumn[] ResolveColumns(
        Type entityType,
        IReadOnlyList<ColumnSchema> columns)
    {
        if (columns.Count > 0)
        {
            return columns
                .Select(column => (ISchemaColumn)new SchemaColumn(column.Name, column.Index, column.Type))
                .ToArray();
        }

        return CreateFallbackColumns(entityType);
    }

    private static ISchemaColumn[] CreateFallbackColumns(Type entityType)
    {
        if (IsScalar(entityType))
            return [new SchemaColumn("Value", 0, entityType)];

        var properties = entityType
            .GetProperties()
            .Where(property => property.CanRead)
            .Where(property => property.GetIndexParameters().Length == 0)
            .ToArray();

        if (properties.Length == 0)
            return [new SchemaColumn("Value", 0, entityType)];

        var columns = new ISchemaColumn[properties.Length];

        for (var index = 0; index < properties.Length; index++)
            columns[index] = new SchemaColumn(properties[index].Name, index, properties[index].PropertyType);

        return columns;
    }

    private static FieldBinding[] CreateFieldBindings(
        string alias,
        IReadOnlyList<ISchemaColumn> columns,
        Func<ISchemaColumn, FieldAccessStrategy> resolveAccessStrategy)
    {
        var fields = new FieldBinding[columns.Count];

        for (var index = 0; index < columns.Count; index++)
        {
            var column = columns[index];
            fields[index] = new FieldBinding(
                column.ColumnName,
                $"{alias}.{column.ColumnName}",
                column.ColumnIndex,
                column.ColumnType,
                FieldNullability.Unknown,
                resolveAccessStrategy(column),
                readModifiers: column.ReadModifiers);
        }

        return fields;
    }

    private static FieldAccessStrategy ResolveAccessStrategy(Type entityType, ISchemaColumn column)
    {
        if (column.ColumnName.Contains('.', StringComparison.Ordinal))
        {
            return new NestedClrPropertyAccess(column.ColumnName);
        }

        if (entityType.GetProperty(column.ColumnName) != null ||
            entityType.GetField(column.ColumnName) != null)
            return new ClrPropertyAccess(column.ColumnName);

        return new PositionalAccess(column.ColumnIndex);
    }
}
