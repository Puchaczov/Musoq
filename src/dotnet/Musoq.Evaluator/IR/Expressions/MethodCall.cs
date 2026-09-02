using System.Collections.Generic;
using System.Reflection;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Expressions;

public sealed record MethodCall(
    MethodInfo Method,
    IReadOnlyList<IrExpression> Arguments,
    string? Alias,
    Type ReturnType) : IrExpression(ReturnType)
{
    internal EnumIntrinsicKind? EnumIntrinsic { get; init; }

    internal EnumTypeDescriptor? OperandEnumType { get; init; }

    internal EnumScalarValue? EnumMask { get; init; }
}
