using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionStoreTable(
    ExecutionVariable Table,
    int TableIndex) : ExecutionNode;
