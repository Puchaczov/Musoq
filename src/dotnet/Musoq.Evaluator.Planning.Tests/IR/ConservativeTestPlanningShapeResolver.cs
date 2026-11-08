using System;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.Tests.IR;

internal sealed class ConservativeTestPlanningShapeResolver : IPlanningShapeResolver
{
    public static ConservativeTestPlanningShapeResolver Instance { get; } = new();

    public PlanningRowShape ResolveSourceShape(PhysicalSchemaScanNode scan) =>
        FromSchema(scan.MethodName, scan.Alias, PlanningRowShapeKind.SourceEntity, typeof(object), scan.OutputSchema);

    public PlanningRowShape ResolveCteRefShape(PhysicalCteRefNode cteRef) =>
        FromSchema(cteRef.Alias, cteRef.Alias, PlanningRowShapeKind.TableRow, typeof(object), cteRef.OutputSchema);

    public PlanningRowShape ResolveInterpretSourceShape(PhysicalInterpretSourceNode interpret) =>
        FromSchema(interpret.Alias, interpret.Alias, PlanningRowShapeKind.SourceEntity, typeof(object), interpret.OutputSchema);

    public PlanningRowShape ResolvePropertySourceShape(PhysicalPropertySourceNode property) =>
        FromSchema(property.Alias, property.Alias, PlanningRowShapeKind.SourceEntity, typeof(object), property.OutputSchema);

    public PlanningRowShape ResolveAccessMethodSourceShape(PhysicalAccessMethodSourceNode accessMethod) =>
        FromSchema(accessMethod.Alias, accessMethod.Alias, PlanningRowShapeKind.SourceEntity, typeof(object), accessMethod.OutputSchema);

    private static PlanningRowShape FromSchema(
        string name,
        string alias,
        PlanningRowShapeKind kind,
        Type runtimeType,
        OutputSchema schema)
    {
        return new PlanningRowShape(
            name,
            alias,
            kind,
            runtimeType,
            schema.Columns
                .Select(column => new PlanningField(
                    column.Name,
                    $"{alias}.{column.Name}",
                    column.Index,
                    column.Type,
                    PlanningFieldNullability.Unknown,
                    PlanningFieldAccessKind.Positional))
                .ToArray());
    }
}
