using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record SourcePipeline(PhysicalNode Source, PhysicalFilterNode? Filter);
