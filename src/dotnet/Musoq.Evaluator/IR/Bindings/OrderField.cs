using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Bindings;

public sealed record OrderField(IrExpression Expression, bool Descending, NullOrdering NullOrdering = NullOrdering.Default);
