using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Expressions.CollectionParameters;

namespace Musoq.Evaluator.IR.Analysis;

internal static class IrExpressionDeterminism
{
    public static bool IsDeterministic(IrExpression expression)
    {
        return ExpressionStabilityAnalyzer.IsStable(expression);
    }

    public static bool AreDeterministic(IEnumerable<IrExpression> expressions)
    {
        return expressions.All(IsDeterministic);
    }

    public static bool TryGetFirstBlockedReason(IrExpression expression, out string reason, string subject = "Expression")
    {
        var reasons = new List<string>();
        AddBlockedReasons(expression, reasons, subject);
        if (reasons.Count == 0)
        {
            reason = string.Empty;
            return false;
        }

        reason = reasons[0];
        return true;
    }

    public static void AddBlockedReasons(IrExpression expression, ICollection<string> reasons, string subject = "Expression")
    {
        switch (expression)
        {
            case Literal or WildcardLiteral or RowPresence or ScriptParameterRef or ScriptVariableRef:
                return;
            case ColumnRef column when column.Stability == Schema.ColumnStability.Stable:
                return;
            case ColumnRef:
                reasons.Add($"{subject} reads a volatile column.");
                return;
            case StrictCast strictCast:
                AddBlockedReasons(strictCast.Expression, reasons, subject);
                return;
            case MethodCall methodCall:
                if (methodCall.ReturnType == typeof(void))
                    reasons.Add($"{subject} calls void method {methodCall.Method.Name}.");

                if (TryGetMethodBlockedReason(methodCall.Method, out var methodReason, subject))
                    reasons.Add(methodReason);

                foreach (var argument in methodCall.Arguments)
                    AddBlockedReasons(argument, reasons, subject);
                return;
            case BinaryOp binary:
                AddBlockedReasons(binary.Left, reasons, subject);
                AddBlockedReasons(binary.Right, reasons, subject);
                return;
            case UnaryOp unary:
                AddBlockedReasons(unary.Operand, reasons, subject);
                return;
            case IsNullCheck isNull:
                AddBlockedReasons(isNull.Expression, reasons, subject);
                return;
            case InCheck inCheck:
                AddBlockedReasons(inCheck.Expression, reasons, subject);
                foreach (var value in inCheck.Values)
                    AddBlockedReasons(value, reasons, subject);
                return;
            case CollectionInCheck collectionInCheck:
                AddBlockedReasons(collectionInCheck.Expression, reasons, subject);
                AddBlockedReasons(collectionInCheck.Collection, reasons, subject);
                return;
            case PatternMatch patternMatch:
                AddBlockedReasons(patternMatch.Expression, reasons, subject);
                AddBlockedReasons(patternMatch.Pattern, reasons, subject);
                return;
            case Between between:
                AddBlockedReasons(between.Expression, reasons, subject);
                AddBlockedReasons(between.Low, reasons, subject);
                AddBlockedReasons(between.High, reasons, subject);
                return;
            case CaseWhen caseWhen:
                foreach (var branch in caseWhen.Branches)
                {
                    AddBlockedReasons(branch.Condition, reasons, subject);
                    AddBlockedReasons(branch.Result, reasons, subject);
                }

                if (caseWhen.ElseExpression != null)
                    AddBlockedReasons(caseWhen.ElseExpression, reasons, subject);
                return;
            case Coalesce coalesce:
                foreach (var childExpression in coalesce.Expressions)
                    AddBlockedReasons(childExpression, reasons, subject);
                return;
            case ArrayAccess arrayAccess:
                AddBlockedReasons(arrayAccess.Array, reasons, subject);
                AddBlockedReasons(arrayAccess.Index, reasons, subject);
                return;
            case AggregateRef:
                reasons.Add($"{subject} contains an aggregate reference.");
                return;
            case WindowFunctionRef:
                reasons.Add($"{subject} contains a window function reference.");
                return;
            case CteTableRef:
                reasons.Add($"{subject} contains a table/subquery reference.");
                return;
            default:
                reasons.Add($"{subject} contains unsupported expression {expression.GetType().Name}.");
                return;
        }
    }

    private static bool TryGetMethodBlockedReason(MethodInfo method, out string reason, string subject)
    {
        return ExpressionStabilityAnalyzer.TryGetMethodInstabilityReason(method, subject, out reason);
    }
}
