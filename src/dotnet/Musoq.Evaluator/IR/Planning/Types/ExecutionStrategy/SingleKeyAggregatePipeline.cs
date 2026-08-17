using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record SingleKeyAggregatePipeline(
    PhysicalProjectNode Project,
    PhysicalSingleKeyAggregateNode Aggregate,
    PhysicalNode Source,
    PhysicalFilterNode? SourceFilter,
    IReadOnlyList<PhysicalNode> PostOperations);
