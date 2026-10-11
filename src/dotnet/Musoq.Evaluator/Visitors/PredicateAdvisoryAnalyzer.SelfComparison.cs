using System.Linq;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

internal static partial class PredicateAdvisoryAnalyzer
{
    private static bool TryReportSelfComparison(SemanticAdvisoryContext context, Node node)
    {
        if (node is not BinaryNode { Left: AccessColumnNode left, Right: AccessColumnNode right } ||
            !IsComparison(node) ||
            !string.Equals(ColumnKey(left), ColumnKey(right), StringComparison.Ordinal) ||
            left.ReturnType != right.ReturnType ||
            !IsSafeSelfComparisonType(left.ReturnType) ||
            !IsStableDirectColumn(context, left))
            return false;

        var alwaysTrue = node is EqualityNode or GreaterOrEqualNode or LessOrEqualNode;
        var code = alwaysTrue
            ? DiagnosticCode.MQ5010_TautologicalCondition
            : DiagnosticCode.MQ5011_ContradictoryCondition;
        var message = alwaysTrue
            ? "Predicate compares a stable non-nullable column with itself and always evaluates to true."
            : "Predicate compares a stable non-nullable column with itself and always evaluates to false.";
        context.Report(code, message, node.Span);
        return true;
    }

    private static bool IsSafeSelfComparisonType(Type type)
    {
        if (!IsStaticallyNonNullable(type) || type == typeof(float) || type == typeof(double))
            return false;

        return type.IsPrimitive || type == typeof(decimal) || type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(Guid);
    }

    private static bool IsStableDirectColumn(SemanticAdvisoryContext context, AccessColumnNode column)
    {
        var columns = context.Metadata.InferredColumns
            .Where(entry => string.Equals(entry.Key.Alias, column.Alias, StringComparison.Ordinal))
            .SelectMany(entry => entry.Value)
            .Concat(context.Metadata.InferredColumnsByAlias
                .Where(entry => string.Equals(entry.Key, column.Alias, StringComparison.Ordinal))
                .SelectMany(entry => entry.Value))
            .Where(candidate => string.Equals(candidate.ColumnName, column.Name, StringComparison.Ordinal))
            .ToArray();
        return columns.Length > 0 && columns.All(candidate => candidate.Stability == ColumnStability.Stable);
    }
}
