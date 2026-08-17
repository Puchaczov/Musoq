namespace Musoq.Evaluator.IR.Execution;

internal sealed record CteReferenceClassification(
    string Name,
    int ReferenceCount,
    CteOutputFlags Flags);
