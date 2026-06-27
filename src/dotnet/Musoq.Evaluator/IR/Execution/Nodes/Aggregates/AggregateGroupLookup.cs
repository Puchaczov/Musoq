using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record AggregateGroupLookup(
    ExecutionVariable Variable,
    int PrefixLength);
