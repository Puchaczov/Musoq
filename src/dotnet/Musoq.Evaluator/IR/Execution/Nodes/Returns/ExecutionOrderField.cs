using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionOrderField(
    string FieldName,
    int OutputIndex,
    Type Type,
    bool Descending, Musoq.Evaluator.IR.Bindings.NullOrdering NullOrdering = Musoq.Evaluator.IR.Bindings.NullOrdering.Default);
