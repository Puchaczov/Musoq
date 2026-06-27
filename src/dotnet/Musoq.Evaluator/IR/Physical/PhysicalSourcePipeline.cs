using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Physical;

internal readonly record struct PhysicalSourcePipeline(
    PhysicalNode Source,
    PhysicalFilterNode? Filter);
