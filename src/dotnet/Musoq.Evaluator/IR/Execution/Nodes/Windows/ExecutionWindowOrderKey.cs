using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionWindowOrderKey(
    ExecutionExpression Expression,
    bool Descending, Musoq.Evaluator.IR.Bindings.NullOrdering NullOrdering = Musoq.Evaluator.IR.Bindings.NullOrdering.Default);
