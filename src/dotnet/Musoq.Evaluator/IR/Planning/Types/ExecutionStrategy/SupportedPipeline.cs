using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record SupportedPipeline(
    PhysicalProjectNode Project,
    PhysicalNode Source,
    PhysicalFilterNode? Filter,
    IReadOnlyList<PhysicalNode> PostOperations);
