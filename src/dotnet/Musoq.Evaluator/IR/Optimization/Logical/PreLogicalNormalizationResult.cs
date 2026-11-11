using System.Collections.Generic;
using Musoq.Evaluator.IR.Optimization.Logical.Subqueries;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.IR.Optimization.Logical;

internal sealed record PreLogicalNormalizationResult(
    RootNode InitialRoot,
    RootNode NormalizedRoot,
    OptimizationTrace Trace,
    IReadOnlyList<LogicalSubqueryOwnershipFact> LogicalSubqueryFacts,
    IReadOnlyList<CorrelatedSubqueryDecision> CorrelatedSubqueryDecisions
);
