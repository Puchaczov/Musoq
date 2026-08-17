namespace Musoq.Evaluator.IR.Execution;

internal sealed record ExecutionCteIndexOnlyStorageCandidate(
    string TableName,
    string RowTypeName,
    bool KeepPayloadRows) : ExecutionNode;
