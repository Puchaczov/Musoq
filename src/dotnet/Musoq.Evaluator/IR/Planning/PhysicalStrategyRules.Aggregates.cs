using System.Linq;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class PhysicalStrategyRules
{
    public static AggregateStrategyDecision ChooseAggregateStrategy(IrExpression[] groupKeys, Type[] groupKeyTypes)
    {
        ArgumentNullException.ThrowIfNull(groupKeyTypes);
        if (groupKeys.Length == 0)
            return new AggregateStrategyDecision(AggregateStrategyKind.AggregateOnly, "No group keys exist.");

        if (groupKeyTypes is [not null])
            return new AggregateStrategyDecision(
                AggregateStrategyKind.SingleKey,
                "One resolved group key exists.",
                IsConstantGroupingKey(groupKeys[0])
            );

        if (groupKeyTypes.Length > 1 && Array.TrueForAll(groupKeyTypes, static keyType => keyType != null))
            return new AggregateStrategyDecision(
                AggregateStrategyKind.ValueTuple,
                "Multiple resolved group keys exist.",
                groupKeys.All(IsConstantGroupingKey)
            );

        return new AggregateStrategyDecision(
            AggregateStrategyKind.Unsupported,
            "Aggregate group key types must be resolved before physical planning."
        );
    }

    private static bool IsConstantGroupingKey(IrExpression key) =>
        key switch
        {
            Literal or ScriptParameterRef or ScriptVariableRef => true,
            StrictCast cast => IsConstantGroupingKey(cast.Expression),
            BinaryOp binary => IsConstantGroupingKey(binary.Left) && IsConstantGroupingKey(binary.Right),
            UnaryOp unary => IsConstantGroupingKey(unary.Operand),
            IsNullCheck check => IsConstantGroupingKey(check.Expression),
            CaseWhen or InCheck or MethodCall => IrExpressionDeterminism.IsDeterministic(key)
                && IrExpressionTraversal.Children(key).All(IsConstantGroupingKey),
            _ => false,
        };
}
