using Musoq.Schema;

namespace Musoq.Evaluator.IR.Expressions;

public abstract record IrExpression(Type ReturnType)
{
    public EnumTypeDescriptor? EnumType { get; init; }
}
