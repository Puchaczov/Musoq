using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.Utils;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

internal sealed class ExecutionPlanningShapeResolverAdapter(ExecutionShapeResolver resolver)
    : IPlanningShapeResolver
{
    public static ExecutionPlanningShapeResolverAdapter Create(
        Scope? scope = null,
        IReadOnlyDictionary<string, ISchemaColumn[]>? inferredColumns = null,
        IReadOnlyDictionary<string, Type>? entityTypesByAlias = null,
        SchemaRegistry? schemaRegistry = null)
    {
        return new ExecutionPlanningShapeResolverAdapter(new ExecutionShapeResolver(
            scope,
            inferredColumns,
            entityTypesByAlias,
            schemaRegistry));
    }

    public PlanningRowShape ResolveSourceShape(PhysicalSchemaScanNode scan) =>
        ToPlanningRowShape(ResolveExecutionSourceShape(scan));

    public PlanningRowShape ResolveCteRefShape(PhysicalCteRefNode cteRef) =>
        ToPlanningRowShape(ResolveExecutionCteRefShape(cteRef));

    public PlanningRowShape ResolveInterpretSourceShape(PhysicalInterpretSourceNode interpret) =>
        ToPlanningRowShape(ResolveExecutionInterpretSourceShape(interpret));

    public PlanningRowShape ResolvePropertySourceShape(PhysicalPropertySourceNode property) =>
        ToPlanningRowShape(ResolveExecutionPropertySourceShape(property));

    public PlanningRowShape ResolveAccessMethodSourceShape(PhysicalAccessMethodSourceNode accessMethod) =>
        ToPlanningRowShape(ResolveExecutionAccessMethodSourceShape(accessMethod));

    private RowShape ResolveExecutionSourceShape(PhysicalSchemaScanNode scan) =>
        resolver.ResolveSourceShape(scan);

    private static RowShape ResolveExecutionCteRefShape(PhysicalCteRefNode cteRef) =>
        new TableRowShape(
            cteRef.Alias,
            cteRef.OutputSchema.Columns.Select(column => new FieldBinding(
                column.Name,
                $"{cteRef.Alias}.{column.Name}",
                column.Index,
                column.Type,
                FieldNullability.Unknown,
                new PositionalAccess(column.Index))).ToArray());

    private RowShape ResolveExecutionInterpretSourceShape(PhysicalInterpretSourceNode interpret) =>
        resolver.ResolveInterpretSourceShape(interpret);

    private RowShape ResolveExecutionPropertySourceShape(PhysicalPropertySourceNode property) =>
        resolver.ResolvePropertySourceShape(property);

    private RowShape ResolveExecutionAccessMethodSourceShape(PhysicalAccessMethodSourceNode accessMethod) =>
        resolver.ResolveAccessMethodSourceShape(accessMethod);

    private static PlanningRowShape ToPlanningRowShape(RowShape shape)
    {
        var alias = RowShapeLookup.TryResolveSourceAlias(shape, out var resolvedAlias)
            ? resolvedAlias
            : shape.Name;

        return new PlanningRowShape(
            shape.Name,
            alias,
            ResolveKind(shape),
            RowShapeLookup.ResolveSourceRuntimeType(shape),
            shape.Fields.Select(ToPlanningField).ToArray());
    }

    private static PlanningField ToPlanningField(FieldBinding field)
    {
        return new PlanningField(
            field.Name,
            field.QualifiedName,
            field.OutputIndex,
            field.Type.ResolveClrType(),
            ResolveNullability(field.Nullability),
            ResolveAccessKind(field.AccessStrategy),
            field.PublicType?.ResolveClrType());
    }

    private static PlanningFieldNullability ResolveNullability(FieldNullability nullability)
    {
        return nullability switch
        {
            FieldNullability.Nullable => PlanningFieldNullability.Nullable,
            FieldNullability.NotNullable => PlanningFieldNullability.NotNullable,
            _ => PlanningFieldNullability.Unknown
        };
    }

    private static PlanningRowShapeKind ResolveKind(RowShape shape)
    {
        return shape switch
        {
            SourceEntityShape => PlanningRowShapeKind.SourceEntity,
            TableRowShape => PlanningRowShapeKind.TableRow,
            ValuesRowShape => PlanningRowShapeKind.Values,
            ExpandoAdapterShape => PlanningRowShapeKind.ExpandoAdapter,
            GeneratedRowShape => PlanningRowShapeKind.Generated,
            _ => PlanningRowShapeKind.Generated
        };
    }

    private static PlanningFieldAccessKind ResolveAccessKind(FieldAccessStrategy access)
    {
        return access switch
        {
            ClrPropertyAccess => PlanningFieldAccessKind.ClrMember,
            ReflectedMemberAccess => PlanningFieldAccessKind.ReflectedMember,
            PositionalAccess => PlanningFieldAccessKind.Positional,
            ExpandoDictionaryAccess => PlanningFieldAccessKind.ExpandoDictionary,
            GeneratedFieldAccess or GeneratedRowTypeAccess => PlanningFieldAccessKind.GeneratedField,
            GeneratedRowContextAccess or ContextAccess => PlanningFieldAccessKind.GeneratedContext,
            NestedClrPropertyAccess => PlanningFieldAccessKind.NestedClrMember,
            NestedPositionalAccess => PlanningFieldAccessKind.NestedPositional,
            GeneratedRowNestedAccess => PlanningFieldAccessKind.GeneratedNested,
            DirectScalarValueAccess => PlanningFieldAccessKind.DirectScalar,
            ApplyOrdinalityAccess => PlanningFieldAccessKind.ApplyOrdinality,
            _ => PlanningFieldAccessKind.Unknown
        };
    }
}
