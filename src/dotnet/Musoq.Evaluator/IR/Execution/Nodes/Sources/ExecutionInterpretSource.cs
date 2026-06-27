using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionInterpretSource(
    ExecutionVariable Rows,
    string SchemaName,
    string InterpreterTypeName,
    InterpretSourceKind Kind,
    IReadOnlyList<ExecutionExpression> Arguments,
    ApplyKind ApplyKind) : ExecutionNode;
