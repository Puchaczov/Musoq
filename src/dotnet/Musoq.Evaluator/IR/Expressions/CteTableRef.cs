using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Expressions;

public sealed record CteTableRef(string Name) : IrExpression(typeof(Table));
