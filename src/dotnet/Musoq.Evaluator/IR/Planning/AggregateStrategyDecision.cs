using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record AggregateStrategyDecision(AggregateStrategyKind Kind, string Reason);
