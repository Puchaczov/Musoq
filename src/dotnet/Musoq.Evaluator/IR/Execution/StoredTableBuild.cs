using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

internal sealed record StoredTableBuild(
    int TableIndex,
    IReadOnlyList<ExecutionNode> Nodes,
    ExecutionVariable Table,
    IReadOnlyList<CapturedLocal> Captures);
