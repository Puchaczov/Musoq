using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using ColumnRefExtractor = Musoq.Evaluator.IR.Expressions.ColumnRefExtractor;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static BuildResult<AsOfJoinPredicateParts> ExtractAsOfJoinPredicate(
        IrExpression predicate,
        string leftAlias,
        string rightAlias)
    {
        var leftAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { leftAlias };
        var rightAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { rightAlias };
        var equalityKeys = new List<JoinKeyExpressions>();
        JoinKeyExpressions? inequalityKey = null;
        var comparisonKind = BinaryOpKind.Equal;
        var conjuncts = new List<IrExpression>();

        CollectJoinConjuncts(predicate, conjuncts);

        foreach (var conjunct in conjuncts)
        {
            if (conjunct is not BinaryOp binary)
            {
                return BuildResult<AsOfJoinPredicateParts>.Unsupported(
                    $"Execution IR ASOF join lowering requires binary predicates, but encountered {conjunct.GetType().Name}.");
            }

            var normalized = NormalizeAsOfJoinKey(binary, leftAliases, rightAliases);
            if (!normalized.Supported)
                return BuildResult<AsOfJoinPredicateParts>.Unsupported(normalized.UnsupportedReason);

            switch (normalized.Value.Kind)
            {
                case BinaryOpKind.Equal:
                    equalityKeys.Add(new JoinKeyExpressions(normalized.Value.Left, normalized.Value.Right));
                    break;
                case BinaryOpKind.GreaterThan:
                case BinaryOpKind.GreaterOrEqual:
                case BinaryOpKind.LessThan:
                case BinaryOpKind.LessOrEqual:
                    if (inequalityKey is not null)
                    {
                        return BuildResult<AsOfJoinPredicateParts>.Unsupported(
                            "Execution IR ASOF join lowering supports exactly one inequality predicate.");
                    }

                    inequalityKey = new JoinKeyExpressions(normalized.Value.Left, normalized.Value.Right);
                    comparisonKind = normalized.Value.Kind;
                    break;
                default:
                    return BuildResult<AsOfJoinPredicateParts>.Unsupported(
                        $"Execution IR ASOF join lowering does not support predicate kind {normalized.Value.Kind}.");
            }
        }

        if (inequalityKey is null)
            return BuildResult<AsOfJoinPredicateParts>.Unsupported("Execution IR ASOF join lowering requires one inequality predicate.");

        return BuildResult<AsOfJoinPredicateParts>.Success(new AsOfJoinPredicateParts(
            equalityKeys.ToArray(),
            inequalityKey.Value.Left,
            inequalityKey.Value.Right,
            comparisonKind));
    }

    private static void CollectJoinConjuncts(IrExpression expression, ICollection<IrExpression> conjuncts)
    {
        if (expression is BinaryOp { Kind: BinaryOpKind.And } and)
        {
            CollectJoinConjuncts(and.Left, conjuncts);
            CollectJoinConjuncts(and.Right, conjuncts);
            return;
        }

        conjuncts.Add(expression);
    }

    private static BuildResult<NormalizedAsOfJoinKey> NormalizeAsOfJoinKey(
        BinaryOp predicate,
        HashSet<string> leftAliases,
        HashSet<string> rightAliases)
    {
        var leftColumns = ColumnRefExtractor.Extract(predicate.Left);
        var rightColumns = ColumnRefExtractor.Extract(predicate.Right);
        var leftUsesLeft = ReferencesAliases(leftColumns, leftAliases);
        var leftUsesRight = ReferencesAliases(leftColumns, rightAliases);
        var rightUsesLeft = ReferencesAliases(rightColumns, leftAliases);
        var rightUsesRight = ReferencesAliases(rightColumns, rightAliases);

        if (leftUsesLeft && !leftUsesRight && rightUsesRight && !rightUsesLeft)
            return BuildResult<NormalizedAsOfJoinKey>.Success(new NormalizedAsOfJoinKey(predicate.Left, predicate.Right, predicate.Kind));

        if (leftUsesRight && !leftUsesLeft && rightUsesLeft && !rightUsesRight)
        {
            return BuildResult<NormalizedAsOfJoinKey>.Success(new NormalizedAsOfJoinKey(
                predicate.Right,
                predicate.Left,
                SwapComparisonKind(predicate.Kind)));
        }

        return BuildResult<NormalizedAsOfJoinKey>.Unsupported(
            "Execution IR ASOF join lowering requires each predicate to reference exactly one left-side and one right-side expression.");
    }

    private static bool ReferencesAliases(IReadOnlyList<ColumnRef> columns, HashSet<string> aliases)
    {
        foreach (var column in columns)
        {
            if (aliases.Contains(column.Alias))
                return true;
        }

        return false;
    }

    private static BinaryOpKind SwapComparisonKind(BinaryOpKind kind)
    {
        return kind switch
        {
            BinaryOpKind.Equal => BinaryOpKind.Equal,
            BinaryOpKind.GreaterThan => BinaryOpKind.LessThan,
            BinaryOpKind.GreaterOrEqual => BinaryOpKind.LessOrEqual,
            BinaryOpKind.LessThan => BinaryOpKind.GreaterThan,
            BinaryOpKind.LessOrEqual => BinaryOpKind.GreaterOrEqual,
            _ => throw new NotSupportedException($"Execution IR ASOF join lowering cannot swap comparison kind {kind}.")
        };
    }
}
