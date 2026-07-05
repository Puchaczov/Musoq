using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static Dictionary<string, IrExpression> CreateProducerProjectionExpressionMap(
        IReadOnlyList<ProjectedField> fields)
    {
        var expressions = new Dictionary<string, IrExpression>(StringComparer.OrdinalIgnoreCase);
        var ambiguousNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in fields)
        {
            AddProducerProjectionExpression(expressions, ambiguousNames, field.OutputName, field.Expression);
            AddProducerProjectionExpression(expressions, ambiguousNames, GetUnqualifiedProjectionName(field.OutputName), field.Expression);
            AddProducerProjectionExpression(expressions, ambiguousNames, IrExpressionPrinter.Print(field.Expression), field.Expression);

            if (field.Expression is ColumnRef columnRef)
            {
                AddProducerProjectionExpression(
                    expressions,
                    ambiguousNames,
                    CreateQualifiedColumnName(columnRef),
                    field.Expression);
            }
        }

        return expressions;
    }

    private static void AddProducerProjectionExpression(
        Dictionary<string, IrExpression> expressions,
        HashSet<string> ambiguousNames,
        string name,
        IrExpression expression)
    {
        if (string.IsNullOrWhiteSpace(name) || ambiguousNames.Contains(name))
            return;

        if (!expressions.TryGetValue(name, out var existingExpression))
        {
            expressions[name] = expression;
            return;
        }

        if (Equals(existingExpression, expression))
            return;

        expressions.Remove(name);
            ambiguousNames.Add(name);
    }

    private static string GetUnqualifiedProjectionName(string fieldName)
    {
        var separatorIndex = fieldName.LastIndexOf('.');
        return separatorIndex < 0 ? fieldName : fieldName[(separatorIndex + 1)..];
    }

    private static IrExpression? RewriteFinalJoinExpression(
        IrExpression expression,
        IReadOnlyDictionary<string, IrExpression> projectedExpressions,
        PhysicalCteRefNode cteRef)
    {
        return expression switch
        {
            ColumnRef columnRef => RewriteFinalJoinColumnRef(columnRef, projectedExpressions, cteRef),
            Literal or WildcardLiteral or RowPresence => expression,
            BinaryOp binary => RewriteBinaryFinalJoinExpression(binary, projectedExpressions, cteRef),
            UnaryOp unary => RewriteUnaryFinalJoinExpression(unary, projectedExpressions, cteRef),
            MethodCall methodCall => RewriteMethodFinalJoinExpression(methodCall, projectedExpressions, cteRef),
            IsNullCheck isNull => RewriteIsNullFinalJoinExpression(isNull, projectedExpressions, cteRef),
            InCheck inCheck => RewriteInFinalJoinExpression(inCheck, projectedExpressions, cteRef),
            PatternMatch pattern => RewritePatternFinalJoinExpression(pattern, projectedExpressions, cteRef),
            Between between => RewriteBetweenFinalJoinExpression(between, projectedExpressions, cteRef),
            CaseWhen caseWhen => RewriteCaseWhenFinalJoinExpression(caseWhen, projectedExpressions, cteRef),
            Coalesce coalesce => RewriteCoalesceFinalJoinExpression(coalesce, projectedExpressions, cteRef),
            ArrayAccess arrayAccess => RewriteArrayAccessFinalJoinExpression(arrayAccess, projectedExpressions, cteRef),
            _ => null
        };
    }

    private static IrExpression? RewriteFinalJoinColumnRef(
        ColumnRef columnRef,
        IReadOnlyDictionary<string, IrExpression> projectedExpressions,
        PhysicalCteRefNode cteRef)
    {
        var qualifiedName = CreateQualifiedColumnName(columnRef);
        if (projectedExpressions.TryGetValue(qualifiedName, out var qualifiedExpression))
            return qualifiedExpression;

        if (projectedExpressions.TryGetValue(columnRef.ColumnName, out var columnExpression))
            return columnExpression;

        return IsCteColumnRef(columnRef, cteRef) ? null : columnRef;
    }

    private static string CreateQualifiedColumnName(ColumnRef columnRef)
    {
        return string.IsNullOrWhiteSpace(columnRef.Alias)
            ? columnRef.ColumnName
            : $"{columnRef.Alias}.{columnRef.ColumnName}";
    }

    private static bool IsCteColumnRef(ColumnRef columnRef, PhysicalCteRefNode cteRef)
    {
        return string.IsNullOrWhiteSpace(columnRef.Alias) ||
               string.Equals(columnRef.Alias, cteRef.Alias, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(columnRef.Alias, cteRef.CteName, StringComparison.OrdinalIgnoreCase);
    }

    private static IrExpression? RewriteBinaryFinalJoinExpression(
        BinaryOp binary,
        IReadOnlyDictionary<string, IrExpression> projectedExpressions,
        PhysicalCteRefNode cteRef)
    {
        var left = RewriteFinalJoinExpression(binary.Left, projectedExpressions, cteRef);
        var right = RewriteFinalJoinExpression(binary.Right, projectedExpressions, cteRef);

        return left == null || right == null
            ? null
            : binary with { Left = left, Right = right };
    }

    private static IrExpression? RewriteUnaryFinalJoinExpression(
        UnaryOp unary,
        IReadOnlyDictionary<string, IrExpression> projectedExpressions,
        PhysicalCteRefNode cteRef)
    {
        var operand = RewriteFinalJoinExpression(unary.Operand, projectedExpressions, cteRef);

        return operand == null ? null : unary with { Operand = operand };
    }

    private static IrExpression? RewriteMethodFinalJoinExpression(
        MethodCall methodCall,
        IReadOnlyDictionary<string, IrExpression> projectedExpressions,
        PhysicalCteRefNode cteRef)
    {
        var arguments = RewriteFinalJoinExpressions(methodCall.Arguments, projectedExpressions, cteRef);

        return arguments == null ? null : methodCall with { Arguments = arguments };
    }

    private static IrExpression? RewriteIsNullFinalJoinExpression(
        IsNullCheck isNull,
        IReadOnlyDictionary<string, IrExpression> projectedExpressions,
        PhysicalCteRefNode cteRef)
    {
        var expression = RewriteFinalJoinExpression(isNull.Expression, projectedExpressions, cteRef);

        return expression == null ? null : isNull with { Expression = expression };
    }

    private static IrExpression? RewriteInFinalJoinExpression(
        InCheck inCheck,
        IReadOnlyDictionary<string, IrExpression> projectedExpressions,
        PhysicalCteRefNode cteRef)
    {
        var expression = RewriteFinalJoinExpression(inCheck.Expression, projectedExpressions, cteRef);
        var values = RewriteFinalJoinExpressions(inCheck.Values, projectedExpressions, cteRef);

        return expression == null || values == null
            ? null
            : inCheck with { Expression = expression, Values = values };
    }

    private static IrExpression? RewritePatternFinalJoinExpression(
        PatternMatch pattern,
        IReadOnlyDictionary<string, IrExpression> projectedExpressions,
        PhysicalCteRefNode cteRef)
    {
        var expression = RewriteFinalJoinExpression(pattern.Expression, projectedExpressions, cteRef);
        var patternExpression = RewriteFinalJoinExpression(pattern.Pattern, projectedExpressions, cteRef);

        return expression == null || patternExpression == null
            ? null
            : pattern with { Expression = expression, Pattern = patternExpression };
    }

    private static IrExpression? RewriteBetweenFinalJoinExpression(
        Between between,
        IReadOnlyDictionary<string, IrExpression> projectedExpressions,
        PhysicalCteRefNode cteRef)
    {
        var expression = RewriteFinalJoinExpression(between.Expression, projectedExpressions, cteRef);
        var low = RewriteFinalJoinExpression(between.Low, projectedExpressions, cteRef);
        var high = RewriteFinalJoinExpression(between.High, projectedExpressions, cteRef);

        return expression == null || low == null || high == null
            ? null
            : between with { Expression = expression, Low = low, High = high };
    }

    private static IrExpression? RewriteCaseWhenFinalJoinExpression(
        CaseWhen caseWhen,
        IReadOnlyDictionary<string, IrExpression> projectedExpressions,
        PhysicalCteRefNode cteRef)
    {
        var branches = new CaseWhenBranch[caseWhen.Branches.Length];
        for (var index = 0; index < caseWhen.Branches.Length; index++)
        {
            var branch = caseWhen.Branches[index];
            var condition = RewriteFinalJoinExpression(branch.Condition, projectedExpressions, cteRef);
            var result = RewriteFinalJoinExpression(branch.Result, projectedExpressions, cteRef);
            if (condition == null || result == null)
                return null;

            branches[index] = branch with { Condition = condition, Result = result };
        }

        if (caseWhen.ElseExpression == null)
            return caseWhen with { Branches = branches };

        var elseExpression = RewriteFinalJoinExpression(caseWhen.ElseExpression, projectedExpressions, cteRef);
        return elseExpression == null
            ? null
            : caseWhen with { Branches = branches, ElseExpression = elseExpression };
    }

    private static IrExpression? RewriteCoalesceFinalJoinExpression(
        Coalesce coalesce,
        IReadOnlyDictionary<string, IrExpression> projectedExpressions,
        PhysicalCteRefNode cteRef)
    {
        var expressions = RewriteFinalJoinExpressions(coalesce.Expressions, projectedExpressions, cteRef);

        return expressions == null
            ? null
            : coalesce with { Expressions = expressions.ToArray() };
    }

    private static IrExpression? RewriteArrayAccessFinalJoinExpression(
        ArrayAccess arrayAccess,
        IReadOnlyDictionary<string, IrExpression> projectedExpressions,
        PhysicalCteRefNode cteRef)
    {
        var array = RewriteFinalJoinExpression(arrayAccess.Array, projectedExpressions, cteRef);
        var index = RewriteFinalJoinExpression(arrayAccess.Index, projectedExpressions, cteRef);

        return array == null || index == null
            ? null
            : arrayAccess with { Array = array, Index = index };
    }

    private static IrExpression[]? RewriteFinalJoinExpressions(
        IReadOnlyList<IrExpression> expressions,
        IReadOnlyDictionary<string, IrExpression> projectedExpressions,
        PhysicalCteRefNode cteRef)
    {
        var rewritten = new IrExpression[expressions.Count];
        for (var index = 0; index < expressions.Count; index++)
        {
            var expression = RewriteFinalJoinExpression(expressions[index], projectedExpressions, cteRef);
            if (expression == null)
                return null;

            rewritten[index] = expression;
        }

        return rewritten;
    }
}
