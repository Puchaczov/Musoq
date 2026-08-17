using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Logical.Nodes;

public sealed record UnpivotEntry(string NameValue, IrExpression Value);
