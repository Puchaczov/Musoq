namespace Musoq.Evaluator.IR.Execution;

public sealed record AggregateGroupOwnerField(
    int PrefixLength,
    string FieldName,
    AggregateGroupShape Shape);
