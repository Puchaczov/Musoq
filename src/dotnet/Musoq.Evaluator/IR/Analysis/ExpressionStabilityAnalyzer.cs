using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Expressions.CollectionParameters;
using Musoq.Plugins.Attributes;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Analysis;

/// <summary>
///     Central stability facts used by transformations that can change scalar evaluation count or timing.
/// </summary>
internal static class ExpressionStabilityAnalyzer
{
    public static bool IsStable(IrExpression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        return expression switch
        {
            Literal or WildcardLiteral or RowPresence or ScriptParameterRef or ScriptVariableRef => true,
            ColumnRef column => column.Stability == ColumnStability.Stable,
            StrictCast cast => IsStable(cast.Expression),
            BinaryOp binary => IsStable(binary.Left) && IsStable(binary.Right),
            UnaryOp unary => IsStable(unary.Operand),
            MethodCall method => method.ReturnType != typeof(void) &&
                                 IsStableMethod(method.Method) &&
                                 method.Arguments.All(IsStable),
            IsNullCheck check => IsStable(check.Expression),
            InCheck check => IsStable(check.Expression) && check.Values.All(IsStable),
            CollectionInCheck check => IsStable(check.Expression) && IsStable(check.Collection),
            PatternMatch match => IsStable(match.Expression) && IsStable(match.Pattern),
            Between between => IsStable(between.Expression) &&
                               IsStable(between.Low) &&
                               IsStable(between.High),
            CaseWhen caseWhen => caseWhen.Branches.All(branch => IsStable(branch.Condition) && IsStable(branch.Result)) &&
                                 (caseWhen.ElseExpression == null || IsStable(caseWhen.ElseExpression)),
            Coalesce coalesce => coalesce.Expressions.All(IsStable),
            ArrayAccess access => IsStable(access.Array) && IsStable(access.Index),
            AggregateRef or WindowFunctionRef or CteTableRef => false,
            _ => false
        };
    }

    public static bool IsStable(ExecutionExpression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        return expression switch
        {
            ExecutionLiteral or ExecutionVariableRead or ExecutionScriptVariableRead => true,
            ExecutionFieldRead field => field.Stability == ColumnStability.Stable,
            ExecutionMemberRead member => IsStableMember(member),
            ExecutionMethodTargetReuseCandidate candidate => IsStable(candidate.MethodCall),
            ExecutionMethodCall call => call.InjectedSource == null &&
                                         call.Method.Descriptor.IsStable &&
                                         call.Arguments.All(IsStable),
            ExecutionStrictCast cast => IsStable(cast.Expression),
            ExecutionBinary binary => IsStable(binary.Left) && IsStable(binary.Right),
            ExecutionUnary unary => IsStable(unary.Operand),
            ExecutionArrayAccess access => IsStable(access.Array) && IsStable(access.Index),
            ExecutionIsNullCheck check => IsStable(check.Expression),
            ExecutionInCheck check => IsStable(check.Expression) && check.Values.All(IsStable),
            ExecutionCollectionInCheck check => IsStable(check.Expression) && IsStable(check.Collection),
            ExecutionPatternMatch match => IsStable(match.Expression) && IsStable(match.Pattern),
            ExecutionBetween between => IsStable(between.Expression) &&
                                        IsStable(between.Low) &&
                                        IsStable(between.High),
            ExecutionCaseWhen caseWhen => caseWhen.Branches.All(branch => IsStable(branch.Condition) && IsStable(branch.Result)) &&
                                          (caseWhen.ElseExpression == null || IsStable(caseWhen.ElseExpression)),
            ExecutionCoalesce coalesce => coalesce.Expressions.All(IsStable),
            ExecutionCompositeKey key => key.Parts.All(IsStable),
            ExecutionValueTupleKey key => key.Parts.All(IsStable),
            ExecutionRowPresence presence => IsStable(presence.PresenceSource),
            ExecutionAggregateCall aggregate => aggregate.Method.Descriptor.IsStable && aggregate.Arguments.All(IsStable),
            ExecutionStoredTable or ExecutionStoredTableRows or ExecutionRowStream or ExecutionScalarRowStream => false,
            _ => false
        };
    }

    public static bool IsStableMethod(MethodInfo method)
    {
        ArgumentNullException.ThrowIfNull(method);

        return !TryGetMethodInstabilityReason(method, "Expression", out _);
    }

    public static bool IsStableMethod(ExecutionCallableRef method) =>
        method.Descriptor.IsStable;

    public static bool TryGetMethodInstabilityReason(
        MethodInfo method,
        string subject,
        out string reason)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        if (method.GetCustomAttribute<NonDeterministicAttribute>() != null)
        {
            reason = $"{subject} calls non-deterministic method {method.Name}.";
            return true;
        }

        foreach (var parameter in method.GetParameters())
        {
            if (parameter.GetCustomAttribute<InjectQueryStatsAttribute>() != null)
            {
                reason = $"{subject} calls {method.Name}, which injects query statistics.";
                return true;
            }

            if (parameter.GetCustomAttribute<InjectSpecificSourceAttribute>() != null ||
                parameter.GetCustomAttribute<InjectSourceAttribute>() != null)
            {
                reason = $"{subject} calls {method.Name}, which injects a source row.";
                return true;
            }

            if (parameter.GetCustomAttribute<InjectTypeAttribute>() != null)
            {
                reason = $"{subject} calls {method.Name}, which injects runtime context.";
                return true;
            }
        }

        reason = string.Empty;
        return false;
    }

    private static bool IsStableMember(ExecutionMemberRead member)
    {
        if (member.IsDynamic || !IsStable(member.Receiver))
            return false;

        var receiverType = Nullable.GetUnderlyingType(member.Receiver.ReturnType.ResolveClrType()) ??
                           member.Receiver.ReturnType.ResolveClrType();
        var property = receiverType.GetProperty(
            member.MemberName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        return property?.GetCustomAttribute<NonDeterministicAttribute>() == null;
    }
}
