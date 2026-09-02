using System.Collections.Generic;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class ExpressionConverter
{
    private static IrExpression ConvertEnumIntrinsic(
        AccessMethodNode node,
        IReadOnlyList<IrExpression> arguments,
        EnumIntrinsicKind intrinsic)
    {
        if (arguments.Count == 0)
            throw new InvalidOperationException($"Enum intrinsic '{intrinsic}' has no bound operand.");

        var operand = arguments[0];
        var descriptor = operand.EnumType ??
                         throw new InvalidOperationException(
                             $"Enum intrinsic '{intrinsic}' lost its logical enum descriptor before IR conversion.");

        if (intrinsic == EnumIntrinsicKind.EnumValue)
            return operand with { EnumType = null };

        EnumScalarValue? mask = null;
        if (intrinsic is EnumIntrinsicKind.HasAnyFlags or EnumIntrinsicKind.HasAllFlags)
        {
            if (arguments.Count != 2 || arguments[1] is not Literal maskLiteral)
                throw new InvalidOperationException($"Enum flags intrinsic '{intrinsic}' has no bound mask literal.");
            mask = CreateEnumScalarValue(descriptor.UnderlyingKind, maskLiteral.Value);
        }

        return new MethodCall(
            node.Method ?? throw new InvalidOperationException($"Enum intrinsic '{intrinsic}' has no marker method."),
            arguments,
            node.Alias,
            RequireReturnType(node))
        {
            EnumIntrinsic = intrinsic,
            OperandEnumType = descriptor,
            EnumMask = mask
        };
    }

    private static EnumScalarValue CreateEnumScalarValue(EnumUnderlyingKind kind, object? value)
    {
        return kind switch
        {
            EnumUnderlyingKind.Byte => EnumScalarValue.FromByte((byte)value!),
            EnumUnderlyingKind.SByte => EnumScalarValue.FromSByte((sbyte)value!),
            EnumUnderlyingKind.Int16 => EnumScalarValue.FromInt16((short)value!),
            EnumUnderlyingKind.UInt16 => EnumScalarValue.FromUInt16((ushort)value!),
            EnumUnderlyingKind.Int32 => EnumScalarValue.FromInt32((int)value!),
            EnumUnderlyingKind.UInt32 => EnumScalarValue.FromUInt32((uint)value!),
            EnumUnderlyingKind.Int64 => EnumScalarValue.FromInt64((long)value!),
            EnumUnderlyingKind.UInt64 => EnumScalarValue.FromUInt64((ulong)value!),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown enum backing kind.")
        };
    }
}
