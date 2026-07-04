using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal interface IPlanningShapeResolver
{
    PlanningRowShape ResolveSourceShape(PhysicalSchemaScanNode scan);

    PlanningRowShape ResolveCteRefShape(PhysicalCteRefNode cteRef);

    PlanningRowShape ResolveInterpretSourceShape(PhysicalInterpretSourceNode interpret);

    PlanningRowShape ResolvePropertySourceShape(PhysicalPropertySourceNode property);

    PlanningRowShape ResolveAccessMethodSourceShape(PhysicalAccessMethodSourceNode accessMethod);
}
