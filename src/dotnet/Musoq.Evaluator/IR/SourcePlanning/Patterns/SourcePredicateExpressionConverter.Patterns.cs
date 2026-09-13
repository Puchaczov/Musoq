using System.Diagnostics.CodeAnalysis;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.IR.SourcePlanning;

internal static partial class SourcePredicateExpressionConverter
{
    private static bool TryConvertStringMatch(
        PatternMatch patternMatch,
        string sourceAlias,
        bool isNegated,
        [NotNullWhen(true)] out SourcePredicateExpression? predicate)
    {
        if (patternMatch.Expression is not ColumnRef column ||
            !string.Equals(column.Alias, sourceAlias, StringComparison.OrdinalIgnoreCase) ||
            patternMatch.Pattern is not Literal { Value: string pattern })
        {
            predicate = null;
            return false;
        }

        var classification = LikePatternClassifier.Classify(pattern);
        if (classification.Classification is not { Domain: LikePatternCharacterDomain.Ascii } match)
        {
            predicate = null;
            return false;
        }

        predicate = new SourcePredicateStringMatch(
            new SourceColumnRef(column.ColumnName),
            match.Kind switch
            {
                LikePatternMatchKind.Exact => SourceStringMatchKind.Exact,
                LikePatternMatchKind.Prefix => SourceStringMatchKind.Prefix,
                LikePatternMatchKind.Suffix => SourceStringMatchKind.Suffix,
                LikePatternMatchKind.Contains => SourceStringMatchKind.Contains,
                _ => throw new InvalidOperationException($"Unknown LIKE classification '{match.Kind}'.")
            },
            pattern,
            pattern.Substring(match.LiteralStart, match.LiteralLength),
            SourceStringComparison.LikeIgnoreCase,
            isNegated);
        return true;
    }
}
