using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed record PreLogicalNormalizationResult(
    RootNode InitialRoot,
    RootNode NormalizedRoot,
    OptimizationTrace Trace,
    IReadOnlyList<LogicalSubqueryOwnershipFact> LogicalSubqueryFacts);
