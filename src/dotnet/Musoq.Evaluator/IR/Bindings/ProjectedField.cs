using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Bindings;

public sealed record ProjectedField(string OutputName, IrExpression Expression, int OutputIndex);
