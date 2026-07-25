using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution.Lowering.Common;

internal sealed record LoweringSourcePipeline(PhysicalNode Source, PhysicalFilterNode? Filter);
