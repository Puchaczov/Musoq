namespace Musoq.Evaluator.IR.Planning;

internal sealed record CteDefinitionStrategyDecision(
    string Name,
    int ReferenceCount,
    CteOutputCharacteristics Characteristics,
    CteDefinitionStrategyKind Kind,
    string Reason)
{
    public bool CanFuseReadOnce => Kind == CteDefinitionStrategyKind.FuseReadOnce;
}
