using Musoq.Schema;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record PlanningField(
    string Name,
    string QualifiedName,
    int OutputIndex,
    Type Type,
    PlanningFieldNullability Nullability,
    PlanningFieldAccessKind AccessKind,
    Type? PublicType = null,
    Type? SourceReadType = null,
    EnumTypeDescriptor? EnumType = null)
{
    public Type ColumnType => PublicType ?? Type;

    public Type EffectiveSourceReadType => SourceReadType ?? ColumnType;
}
