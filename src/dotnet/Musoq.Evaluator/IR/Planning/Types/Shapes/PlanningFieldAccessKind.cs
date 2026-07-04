namespace Musoq.Evaluator.IR.Planning;

internal enum PlanningFieldAccessKind
{
    Unknown,
    ClrMember,
    ReflectedMember,
    Positional,
    ExpandoDictionary,
    GeneratedField,
    GeneratedContext,
    NestedClrMember,
    NestedPositional,
    GeneratedNested,
    DirectScalar,
    ApplyOrdinality
}
