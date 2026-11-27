using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

internal static class ExecutionBinaryLowering
{
    internal static ExecutionExpression Create(BinaryOp binary, ExecutionExpression left, ExecutionExpression right)
    {
        if (binary.Kind == BinaryOpKind.SqlStringConcatenate)
            return new ExecutionStringConcatenation([left, right]);
        var kind = ComparisonKind(
            binary.Kind,
            binary.IsMaterializedMembershipEquality,
            left.ReturnType,
            right.ReturnType
        );
        return new ExecutionBinary(kind, left, right, binary.ReturnType)
        {
            UsesSqlNullSemantics = binary.UsesSqlNullSemantics,
        };
    }

    internal static BinaryOpKind ComparisonKind(
        BinaryOpKind kind,
        bool materializedMembership,
        ExecutionTypeRef left,
        ExecutionTypeRef right
    ) =>
        kind == BinaryOpKind.Equal && materializedMembership && IsFloating(left) && IsFloating(right)
            ? BinaryOpKind.FloatingMembershipEqual
            : kind;

    private static bool IsFloating(ExecutionTypeRef type)
    {
        var clr = type.ResolveClrType();
        return Type.GetTypeCode(Nullable.GetUnderlyingType(clr) ?? clr) is TypeCode.Single or TypeCode.Double;
    }
}
