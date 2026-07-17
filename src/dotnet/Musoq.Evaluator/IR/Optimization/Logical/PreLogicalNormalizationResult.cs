using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.IR.Optimization.Logical.Subqueries;

namespace Musoq.Evaluator.IR.Optimization.Logical;

internal sealed record PreLogicalNormalizationResult(
    RootNode InitialRoot,
    RootNode NormalizedRoot,
    OptimizationTrace Trace,
    IReadOnlyList<LogicalSubqueryOwnershipFact> LogicalSubqueryFacts,
    IReadOnlyList<CorrelatedSubqueryDecision> CorrelatedSubqueryDecisions);

