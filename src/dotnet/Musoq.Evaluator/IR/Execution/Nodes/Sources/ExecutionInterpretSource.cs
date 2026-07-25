using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionInterpretSource : ExecutionNode
{
    public ExecutionInterpretSource(
        ExecutionVariable rows,
        string schemaName,
        string interpreterTypeName,
        InterpretSourceKind kind,
        IReadOnlyList<ExecutionExpression> arguments,
        ApplyKind applyKind)
    {
        Rows = rows;
        SchemaName = schemaName;
        InterpreterTypeName = interpreterTypeName;
        Kind = kind;
        Arguments = ExecutionIrCollections.Freeze(arguments);
        ApplyKind = applyKind;
    }

    public ExecutionVariable Rows { get; init; }
    public string SchemaName { get; init; }
    public string InterpreterTypeName { get; init; }
    public InterpretSourceKind Kind { get; init; }
    public IReadOnlyList<ExecutionExpression> Arguments { get; init; }
    public ApplyKind ApplyKind { get; init; }
}
