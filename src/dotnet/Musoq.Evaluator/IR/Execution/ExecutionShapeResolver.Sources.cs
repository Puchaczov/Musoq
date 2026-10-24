using System.Linq;
using Musoq.Evaluator.IR.Logical.Nodes;
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

        if (scan.SourceTransferStrategy is
            {
                Mode: SourceTransferMode.QueryScopedRows,
                Carrier: not null,
                Shape: not null
            } transfer)
        {
            return QueryRowSourceShapeFactory.Create(scan.Alias, transfer);
        }

        var entityType = ResolveEntityType(scan.Alias);
        var columns = ResolveColumns(scan);
        var scalarShape = IsScalar(entityType)
            ? CreateDirectScalarSourceShapeOrNull(scan.Alias, entityType, columns)
            : null;
        if (scalarShape != null)
            return scalarShape;

        if (DynamicEntityBoundary.IsNameIndexedSource(entityType))
            throw new Musoq.Evaluator.Exceptions.SourcePlanContractException(
                $"Source '{scan.Alias}' requires native query-scoped rows; erased dictionary transfer is unsupported.",
                Musoq.Parser.TextSpan.Empty);

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
        if (HasGeneratedInterpreterTypeName(interpret.SchemaName))
        {
            _schemaRegistry!.TryGetSchema(interpret.SchemaName, out var registration);
            var shape = CreateClrMemberSourceShape(
                interpret.Alias,
                columns,
                interpret.Kind is InterpretSourceKind.PartialInterpret or InterpretSourceKind.PartialParse
                    ? $"Musoq.Schema.Interpreters.PartialInterpretResult<{registration?.GeneratedTypeName}>"
                    : registration?.GeneratedTypeName,
                column => column == "ParsedFields" &&
                    interpret.Kind is InterpretSourceKind.PartialInterpret or InterpretSourceKind.PartialParse
                    ? registration?.GeneratedTypeName
                    : InterpretationPropertyTypeNameResolver.ResolvePropertyTypeName(
                    interpret.SchemaName,
                    column,
                    _schemaRegistry));
            if (registration?.Node is not Musoq.Parser.Nodes.InterpretationSchema.TextSchemaNode)
                return shape;
            var result = new TextResultShapeBinding(_schemaRegistry).Schema(interpret.SchemaName);
            var partial = interpret.Kind is InterpretSourceKind.PartialInterpret or InterpretSourceKind.PartialParse;
            return shape with
            {
                Fields = shape.Fields.Select(field =>
                {
                    var value = partial && field.Name == "ParsedFields" ? result : result.Member(field.Name);
                    return value == null ? field : field with
                    {
                        InterpretationShape = new ExecutionInterpretationShape(value),
                        GeneratedTypeName = value.TypeName
                    };
                }).ToArray()
            };
        }

        return CreateSourceShape(interpret.Alias, entityType, columns);
    }

    public RowShape ResolvePropertySourceShape(PhysicalPropertySourceNode property)
    {
        ArgumentNullException.ThrowIfNull(property);
        if (InterpretationPropertyTypeNameResolver.HasGeneratedEnumerableElementType(property, _schemaRegistry))
        {
            var generatedTypeName = InterpretationPropertyTypeNameResolver.ResolveEnumerableTypeName(
                property,
                _schemaRegistry);
            if (generatedTypeName?.EndsWith("[]", StringComparison.Ordinal) == true)
                generatedTypeName = generatedTypeName[..^2];

            return CreateClrMemberSourceShape(
                property.Alias,
                property.OutputSchema.Columns,
                generatedTypeName,
                column => InterpretationPropertyTypeNameResolver.ResolvePropertyTypeNameFromGeneratedType(
                    generatedTypeName!,
                    column,
                    _schemaRegistry));
        }

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
