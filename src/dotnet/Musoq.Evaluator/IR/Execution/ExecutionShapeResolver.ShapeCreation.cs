using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionShapeResolver
{
    private static RowShape CreateSourceShape(
        string alias,
        Type entityType,
        IReadOnlyList<ColumnSchema> columns)
    {
        var resolvedColumns = ResolveColumns(entityType, columns);

        if (IsDynamicEntity(entityType))
        {
            var dynamicFields = CreateFieldBindings(
                alias,
                resolvedColumns,
                column => new ExpandoDictionaryAccess(column.ColumnName));
            return new ExpandoAdapterShape(alias, CreateDynamicTypeName(alias), entityType, dynamicFields);
        }

        if (!CanUseSourceEntityShape(entityType))
            return CreateReflectedSourceShape(alias, entityType, resolvedColumns);

        var fields = CreateFieldBindings(
            alias,
            resolvedColumns,
            column => ResolveAccessStrategy(entityType, column));
        return new SourceEntityShape(alias, entityType, fields);
    }

    private static SourceEntityShape CreateClrMemberSourceShape(
        string alias,
        IReadOnlyList<ColumnSchema> columns,
        string? generatedTypeName = null,
        Func<string, string?>? generatedFieldTypeNameResolver = null)
    {
        var resolvedColumns = ResolveColumns(typeof(object), columns);
        var fields = CreateFieldBindings(
            alias,
            resolvedColumns,
            static column => column.ColumnName.Contains('.', StringComparison.Ordinal)
                ? new NestedClrPropertyAccess(column.ColumnName)
                : new ClrPropertyAccess(column.ColumnName));

        if (generatedFieldTypeNameResolver != null)
        {
            fields = fields
                .Select(field => field with
                {
                    GeneratedTypeName = generatedFieldTypeNameResolver(field.Name) ?? field.GeneratedTypeName
                })
                .ToArray();

        }

        return new SourceEntityShape(alias, typeof(object), fields, generatedTypeName);
    }

    private static SourceEntityShape CreateReflectedSourceShape(
        string alias,
        Type entityType,
        IReadOnlyList<ISchemaColumn> columns)
    {
        var fields = CreateFieldBindings(
            alias,
            columns,
            column => new ReflectedMemberAccess(column.ColumnName));

        return new SourceEntityShape(alias, entityType, fields);
    }

    private static SourceEntityShape? CreateDirectScalarSourceShapeOrNull(
        string alias,
        Type elementType,
        IReadOnlyList<ColumnSchema> columns)
    {
        var resolvedColumns = ResolveColumns(elementType, columns);
        return CreateDirectScalarSourceShapeOrNull(alias, elementType, resolvedColumns);
    }

    private static SourceEntityShape? CreateDirectScalarSourceShapeOrNull(
        string alias,
        Type elementType,
        IReadOnlyList<ISchemaColumn> resolvedColumns)
    {
        if (!CanUseDirectScalarSourceShape(elementType, resolvedColumns))
            return null;

        var fields = CreateFieldBindings(
            alias,
            resolvedColumns,
            static _ => new DirectScalarValueAccess());
        return new SourceEntityShape(alias, elementType, fields);
    }

    private static bool CanUseDirectScalarSourceShape(
        Type elementType,
        IReadOnlyList<ISchemaColumn> columns)
    {
        return CanUseSourceEntityShape(elementType) &&
               columns.Count == 1 &&
               string.Equals(columns[0].ColumnName, nameof(PrimitiveTypeEntity<>.Value), StringComparison.Ordinal);
    }

    private static string CreateDynamicTypeName(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
            return "DynamicRow0";

        return $"{alias}DynamicRow0";
    }
}
