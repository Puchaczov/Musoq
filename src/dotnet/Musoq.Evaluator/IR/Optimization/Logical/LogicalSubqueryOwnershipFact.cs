using System.Collections.Generic;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Logical;

internal sealed record LogicalSubqueryOwnershipFact(
    string CteName,
    LogicalSubqueryFormKind Kind,
    bool IsCorrelated,
    IReadOnlyList<string> OutputColumns,
    string Reason);

