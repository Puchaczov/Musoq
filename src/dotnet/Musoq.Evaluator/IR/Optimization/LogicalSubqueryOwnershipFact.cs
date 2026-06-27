using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed record LogicalSubqueryOwnershipFact(
    string CteName,
    LogicalSubqueryFormKind Kind,
    bool IsCorrelated,
    IReadOnlyList<string> OutputColumns,
    string Reason);
