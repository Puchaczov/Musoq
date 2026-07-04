using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed record SourcePipeline(PhysicalNode Source, PhysicalFilterNode? Filter);
