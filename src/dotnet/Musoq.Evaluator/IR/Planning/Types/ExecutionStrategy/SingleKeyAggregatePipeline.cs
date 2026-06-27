using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning.Cardinality;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record SingleKeyAggregatePipeline(
    PhysicalProjectNode Project,
    PhysicalSingleKeyAggregateNode Aggregate,
    PhysicalNode Source,
    PhysicalFilterNode? SourceFilter,
    IReadOnlyList<PhysicalNode> PostOperations);
