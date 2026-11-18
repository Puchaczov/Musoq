using System.Linq;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;

namespace Musoq.Evaluator.IR.Planning;

internal static class GroupMaximumSelection
{
    internal static GroupMaximumSummary? Select(JoinKind kind, PhysicalNode right, HashJoinDecomposition hash)
    {
        if (
            kind != JoinKind.LeftAntiSemi
            || right is not PhysicalCteRefNode source
            || hash.BuildKeys is not [ColumnRef marker, _, ..]
            || marker.ColumnName != source.CteName + GeneratedSubqueryContract.GreaterAllKeySuffix
            || hash.Residual
                is not BinaryOp
                {
                    Kind: BinaryOpKind.Or,
                    Left: BinaryOp
                    {
                        Kind: BinaryOpKind.Or,
                        Left: IsNullCheck { IsNegated: false, Expression: ColumnRef probe },
                        Right: IsNullCheck { IsNegated: false, Expression: ColumnRef build }
                    },
                    Right: BinaryOp { Kind: BinaryOpKind.LessOrEqual } comparison
                }
            || comparison.Left != probe
            || comparison.Right != build
            || build.Alias != source.Alias
            || probe.Alias == source.Alias
            || !ExpressionStabilityAnalyzer.IsStable(hash.Residual)
            || !hash.BuildKeys.All(ExpressionStabilityAnalyzer.IsStable)
            || !hash.ProbeKeys.All(ExpressionStabilityAnalyzer.IsStable)
        )
            return null;

        var valueType = Nullable.GetUnderlyingType(build.ReturnType) ?? build.ReturnType;
        if (valueType != (Nullable.GetUnderlyingType(probe.ReturnType) ?? probe.ReturnType))
            return null;
        return
            Type.GetTypeCode(valueType)
                is TypeCode.SByte
                    or TypeCode.Byte
                    or TypeCode.Int16
                    or TypeCode.UInt16
                    or TypeCode.Int32
                    or TypeCode.UInt32
                    or TypeCode.Int64
                    or TypeCode.UInt64
                    or TypeCode.Decimal
            && !valueType.IsEnum
            ? new GroupMaximumSummary(probe, build)
            : null;
    }
}
