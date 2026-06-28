using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionPlanPrinter
{    private static string FormatRowValues(IReadOnlyList<ExecutionRowValue> values)
    {
        var builder = new StringBuilder();

        for (var index = 0; index < values.Count; index++)
        {
            if (index > 0)
                builder.Append(", ");

            builder.Append(values[index].FieldName);
            builder.Append(": ");
            builder.Append(FormatExpression(values[index].Value));
        }

        return builder.ToString();
    }

    private static string FormatAsOfPredicate(ExecutionAsOfProbe asOfProbe)
    {
        var predicates = asOfProbe.EqualityKeys
            .Select(key => $"{FormatExpression(key.Left)} = {FormatExpression(key.Right)}")
            .Concat(
            [
                $"{FormatExpression(asOfProbe.ProbeKey)} {FormatBinaryOperator(asOfProbe.ComparisonKind)} {FormatExpression(asOfProbe.CandidateKey)}"
            ]);

        var predicate = string.Join(" and ", predicates);
        return asOfProbe.TieBreak == null
            ? predicate
            : $"{predicate} tie break by {FormatAsOfTieBreak(asOfProbe.TieBreak)}";
    }

    private static string FormatAsOfIndexKey(ExecutionCreateAsOfIndex createIndex)
    {
        var predicates = createIndex.EqualityKeys
            .Select(key => FormatExpression(key.Right))
            .Concat([FormatExpression(createIndex.CandidateKey)]);

        var key = string.Join(", ", predicates);
        return createIndex.TieBreak == null
            ? key
            : $"{key}; tie {FormatAsOfTieBreak(createIndex.TieBreak)}";
    }

    private static string FormatAsOfIndex(ExecutionAsOfProbe asOfProbe)
    {
        return asOfProbe.Index == null
            ? string.Empty
            : $" using {asOfProbe.Index.Name}";
    }

    private static string FormatAsOfTieBreak(ExecutionAsOfTieBreak tieBreak)
    {
        return $"{FormatExpression(tieBreak.Key)}{FormatDirection(tieBreak.Descending)}{FormatNullOrdering(tieBreak.NullOrdering)}";
    }

    private static string FormatSetOperationStrategy(ExecutionSetOperationStrategy strategy)
    {
        return strategy == ExecutionSetOperationStrategy.GeneratedEqualityLoop
            ? string.Empty
            : $", {strategy}";
    }

}
