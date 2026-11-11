using System.Collections.Generic;
using System.Reflection;
using Musoq.Schema;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Expressions;

public sealed record MethodCall(
    MethodInfo Method,
    IReadOnlyList<IrExpression> Arguments,
    string? Alias,
    Type ReturnType) : IrExpression(ReturnType)
{
    internal ByteBufferCallableSemantics? BufferSemantics { get; init; }

    internal ByteBufferValuePlan? BufferPlan { get; init; }

    internal EnumIntrinsicKind? EnumIntrinsic { get; init; }

    internal EnumTypeDescriptor? OperandEnumType { get; init; }

    internal EnumScalarValue? EnumMask { get; init; }
}
