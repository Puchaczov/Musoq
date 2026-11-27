using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using AliasRefExtractor = Musoq.Evaluator.IR.Expressions.AliasRefExtractor;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static LoweringAttempt<AsOfJoinPredicateParts> ExtractAsOfJoinPredicate(
        IrExpression predicate,
        string leftAlias,
        string rightAlias
    )
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
                return LoweringAttempt<AsOfJoinPredicateParts>.Unsupported(
                    $"Execution IR ASOF join lowering requires binary predicates, but encountered {conjunct.GetType().Name}."
                );
            }

            var normalized = NormalizeAsOfJoinKey(binary, leftAliases, rightAliases);
            if (!normalized.IsBuilt)
                return LoweringAttempt<AsOfJoinPredicateParts>.Unsupported(normalized.UnsupportedReason);

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
                        return LoweringAttempt<AsOfJoinPredicateParts>.Unsupported(
                            "Execution IR ASOF join lowering supports exactly one inequality predicate."
                        );
                    }

                    inequalityKey = new JoinKeyExpressions(normalized.Value.Left, normalized.Value.Right);
                    comparisonKind = normalized.Value.Kind;
                    break;
                default:
                    return LoweringAttempt<AsOfJoinPredicateParts>.Unsupported(
                        $"Execution IR ASOF join lowering does not support predicate kind {normalized.Value.Kind}."
                    );
            }
        }

        if (inequalityKey is null)
            return LoweringAttempt<AsOfJoinPredicateParts>.Unsupported(
                "Execution IR ASOF join lowering requires one inequality predicate."
            );

        return LoweringAttempt<AsOfJoinPredicateParts>.Built(
            new AsOfJoinPredicateParts(
                equalityKeys.ToArray(),
                inequalityKey.Value.Left,
                inequalityKey.Value.Right,
                comparisonKind
            )
        );
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

    private static LoweringAttempt<NormalizedAsOfJoinKey> NormalizeAsOfJoinKey(
        BinaryOp predicate,
        HashSet<string> leftAliases,
        HashSet<string> rightAliases
    )
    {
        var leftReferences = AliasRefExtractor.Extract(predicate.Left);
        var rightReferences = AliasRefExtractor.Extract(predicate.Right);
        var leftUsesLeft = leftAliases.Overlaps(leftReferences);
        var leftUsesRight = rightAliases.Overlaps(leftReferences);
        var rightUsesLeft = leftAliases.Overlaps(rightReferences);
        var rightUsesRight = rightAliases.Overlaps(rightReferences);

        if (leftUsesLeft && !leftUsesRight && rightUsesRight && !rightUsesLeft)
            return LoweringAttempt<NormalizedAsOfJoinKey>.Built(
                new NormalizedAsOfJoinKey(predicate.Left, predicate.Right, predicate.Kind)
            );

        if (leftUsesRight && !leftUsesLeft && rightUsesLeft && !rightUsesRight)
        {
            return LoweringAttempt<NormalizedAsOfJoinKey>.Built(
                new NormalizedAsOfJoinKey(predicate.Right, predicate.Left, SwapComparisonKind(predicate.Kind))
            );
        }

        return LoweringAttempt<NormalizedAsOfJoinKey>.Unsupported(
            "Execution IR ASOF join lowering requires each predicate to reference exactly one left-side and one right-side expression."
        );
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
            _ => throw new NotSupportedException(
                $"Execution IR ASOF join lowering cannot swap comparison kind {kind}."
            ),
        };
    }
}
