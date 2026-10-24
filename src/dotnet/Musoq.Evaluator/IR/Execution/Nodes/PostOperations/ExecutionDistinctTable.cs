namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionDistinctTable(
    ExecutionVariable Source,
    ExecutionVariable Target) : ExecutionNode
{
    private System.Collections.Generic.IReadOnlyList<FieldBinding> _keyFields = [];
    public System.Collections.Generic.IReadOnlyList<FieldBinding> KeyFields
    {
        get => _keyFields;
        init => _keyFields = ExecutionIrCollections.Freeze(value);
    }
}
