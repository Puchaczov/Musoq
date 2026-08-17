namespace Musoq.Evaluator.IR.Planning;

internal enum CteDefinitionStrategyKind
{
    Unreferenced,
    FuseReadOnce,
    MaterializeSingleUse,
    MaterializeReuse
}
