using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Optimization.Logical;

internal sealed record LogicalSubqueryOwnershipFact(
    string CteName,
    LogicalSubqueryFormKind Kind,
    bool IsCorrelated,
    IReadOnlyList<string> OutputColumns,
    string Reason);

