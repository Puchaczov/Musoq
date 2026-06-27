using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Plugins;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionShapeResolver
{
    public RowShape ResolveSourceShape(PhysicalSchemaScanNode scan)
    {
        ArgumentNullException.ThrowIfNull(scan);
        var entityType = ResolveEntityType(scan.Alias);
        var columns = ResolveColumns(scan);
        var scalarShape = IsScalar(entityType)
            ? CreateDirectScalarSourceShapeOrNull(scan.Alias, entityType, columns)
            : null;
        if (scalarShape != null)
            return scalarShape;

        if (IsDynamicEntity(entityType))
        {
            var dynamicFields = CreateFieldBindings(
                scan.Alias,
                columns,
                column => new ExpandoDictionaryAccess(column.ColumnName));
            return new ExpandoAdapterShape(scan.Alias, CreateDynamicTypeName(scan.Alias), entityType, dynamicFields);
        }

        if (!CanUseSourceEntityShape(entityType))
            return CreateReflectedSourceShape(scan.Alias, entityType, columns);

        var fields = CreateFieldBindings(
            scan.Alias,
            columns,
            column => ResolveAccessStrategy(entityType, column));
        return new SourceEntityShape(scan.Alias, entityType, fields);
    }

    public RowShape ResolveInterpretSourceShape(PhysicalInterpretSourceNode interpret)
    {
        ArgumentNullException.ThrowIfNull(interpret);
        var entityType = ResolveInterpretEntityType(interpret);
        var columns = ResolveInterpretColumns(interpret);

        if (entityType == typeof(object) && HasGeneratedInterpreterTypeName(interpret.SchemaName))
            return CreateClrMemberSourceShape(interpret.Alias, columns);

        return CreateSourceShape(interpret.Alias, entityType, columns);
    }

    public RowShape ResolvePropertySourceShape(PhysicalPropertySourceNode property)
    {
        ArgumentNullException.ThrowIfNull(property);
        if (InterpretationPropertyTypeNameResolver.HasGeneratedEnumerableElementType(property, _schemaRegistry))
            return CreateClrMemberSourceShape(property.Alias, property.OutputSchema.Columns);

        return ResolveEnumerableSourceShape(property.Alias, property.ResultType, property.OutputSchema.Columns);
    }

    public RowShape ResolveAccessMethodSourceShape(PhysicalAccessMethodSourceNode accessMethod)
    {
        ArgumentNullException.ThrowIfNull(accessMethod);
        return ResolveEnumerableSourceShape(accessMethod.Alias, accessMethod.ResultType, accessMethod.OutputSchema.Columns);
    }

    private static RowShape ResolveEnumerableSourceShape(
        string alias,
        Type resultType,
        IReadOnlyList<ColumnSchema> columns)
    {
        var elementType = ResolveEnumerableElementType(resultType);
        var scalarShape = IsScalar(elementType)
            ? CreateDirectScalarSourceShapeOrNull(alias, elementType, columns)
            : null;
        if (scalarShape != null)
            return scalarShape;

        if (IsScalar(elementType))
            return CreateSourceShape(alias, typeof(PrimitiveTypeEntity<>).MakeGenericType(elementType), columns);

        return CreateSourceShape(alias, elementType, columns);
    }
}
