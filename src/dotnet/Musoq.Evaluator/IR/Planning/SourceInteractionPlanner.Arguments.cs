using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class SourceInteractionPlanner
{
    private static SourceInteractionArguments ResolveArgumentMode(SchemaScanNode scan)
    {
        if (scan.Arguments.Length == 0)
        {
            return new SourceInteractionArguments(
                SourceArgumentMode.ConstantArguments,
                PlanningConfidence.High,
                "Source has no arguments.");
        }

        var references = CollectArgumentReferences(scan.Arguments);
        if (references.HasUnknownExpression)
        {
            return new SourceInteractionArguments(
                SourceArgumentMode.Unknown,
                PlanningConfidence.Low,
                "At least one source argument expression could not be classified.");
        }

        if (references.Aliases.Count == 0)
        {
            return new SourceInteractionArguments(
                SourceArgumentMode.ConstantArguments,
                PlanningConfidence.High,
                "Source arguments do not reference source columns.");
        }

        var externalAliases = references.Aliases
            .Where(alias => !string.Equals(alias, scan.Alias, StringComparison.OrdinalIgnoreCase))
            .OrderBy(static alias => alias, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (externalAliases.Length > 0)
        {
            return new SourceInteractionArguments(
                SourceArgumentMode.CorrelatedArguments,
                PlanningConfidence.High,
                $"Source arguments reference outer alias(es): {string.Join(", ", externalAliases)}.");
        }

        return new SourceInteractionArguments(
            SourceArgumentMode.SourceLocalArguments,
            PlanningConfidence.Medium,
            $"Source arguments reference only alias {scan.Alias}.");
    }

    private static ArgumentReferenceResult CollectArgumentReferences(IReadOnlyList<IrExpression> expressions)
    {
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return CollectArgumentReferences(expressions, aliases);
    }

    private static ArgumentReferenceResult CollectArgumentReferences(
        IReadOnlyList<IrExpression> expressions,
        HashSet<string> aliases)
    {
        var hasUnknownExpression = false;

        foreach (var expression in expressions)
            hasUnknownExpression |= CollectArgumentReferences(expression, aliases);

        return new ArgumentReferenceResult(aliases, hasUnknownExpression);
    }

    private static bool CollectArgumentReferences(IrExpression expression, HashSet<string> aliases)
    {
        switch (expression)
        {
            case Literal or WildcardLiteral:
                return false;
            case ColumnRef column:
                aliases.Add(column.Alias);
                return false;
            case BinaryOp binary:
                return CollectArgumentReferences(binary.Left, aliases) |
                       CollectArgumentReferences(binary.Right, aliases);
            case UnaryOp unary:
                return CollectArgumentReferences(unary.Operand, aliases);
            case MethodCall call:
                return CollectArgumentReferences(call.Arguments, aliases).HasUnknownExpression;
            case InCheck check:
                return CollectArgumentReferences(check.Expression, aliases) |
                       CollectArgumentReferences(check.Values, aliases).HasUnknownExpression;
            case Between between:
                return CollectArgumentReferences(between.Expression, aliases) |
                       CollectArgumentReferences(between.Low, aliases) |
                       CollectArgumentReferences(between.High, aliases);
            case IsNullCheck check:
                return CollectArgumentReferences(check.Expression, aliases);
            case PatternMatch match:
                return CollectArgumentReferences(match.Expression, aliases) |
                       CollectArgumentReferences(match.Pattern, aliases);
            case Coalesce coalesce:
                return CollectArgumentReferences(coalesce.Expressions, aliases).HasUnknownExpression;
            case CaseWhen caseWhen:
                return CollectArgumentReferences(caseWhen, aliases);
            case ArrayAccess access:
                return CollectArgumentReferences(access.Array, aliases) |
                       CollectArgumentReferences(access.Index, aliases);
            default:
                return true;
        }
    }

    private static bool CollectArgumentReferences(CaseWhen caseWhen, HashSet<string> aliases)
    {
        var hasUnknownExpression = false;

        foreach (var branch in caseWhen.Branches)
        {
            hasUnknownExpression |= CollectArgumentReferences(branch.Condition, aliases);
            hasUnknownExpression |= CollectArgumentReferences(branch.Result, aliases);
        }

        if (caseWhen.ElseExpression != null)
            hasUnknownExpression |= CollectArgumentReferences(caseWhen.ElseExpression, aliases);

        return hasUnknownExpression;
    }
}
