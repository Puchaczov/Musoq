using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record PlanningRowShape(
    string Name,
    string Alias,
    PlanningRowShapeKind Kind,
    Type RuntimeType,
    IReadOnlyList<PlanningField> Fields)
{
    public bool IsDynamic => Kind == PlanningRowShapeKind.ExpandoAdapter ||
                             RuntimeType == typeof(object);
}
