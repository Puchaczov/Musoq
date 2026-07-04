namespace Musoq.Evaluator.IR.Planning;

internal sealed record PlanningField(
    string Name,
    string QualifiedName,
    int OutputIndex,
    Type Type,
    PlanningFieldNullability Nullability,
    PlanningFieldAccessKind AccessKind,
    Type? PublicType = null)
{
    public Type ColumnType => PublicType ?? Type;
}
