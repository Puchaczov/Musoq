using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private static List<(ISchemaColumn Column, Node? ReplacementExpression)> ApplyStarModifiers(
        AllColumnsNode node,
        List<ISchemaColumn> eligibleColumns,
        Node[]? inferredReplaceExpressions)
    {
        var span = node.SpanOrEmpty();

        var surviving = ApplyLikeFilter(node, eligibleColumns, span);
        surviving = ApplyExcludeFilter(node, surviving, span);
        return ApplyReplaceSubstitution(node, surviving, eligibleColumns, inferredReplaceExpressions, span);
    }

    private static List<ISchemaColumn> ApplyLikeFilter(
        AllColumnsNode node,
        List<ISchemaColumn> columns,
        TextSpan span)
    {
        if (node.LikePattern == null)
            return [..columns];

        var matcher = CreateLikeColumnMatcher(node.LikePattern);
        var filtered = node.IsNotLike
            ? columns.Where(c => !matcher(c.ColumnName)).ToList()
            : columns.Where(c => matcher(c.ColumnName)).ToList();

        if (filtered.Count == 0)
        {
            var direction = node.IsNotLike ? "NOT LIKE" : "LIKE";
            throw new StarModifierValidationException(
                $"Star modifier {direction} '{node.LikePattern}' matched no columns.",
                DiagnosticCode.MQ3045_StarLikeMatchedNoColumns,
                span);
        }

        return filtered;
    }

    private static List<ISchemaColumn> ApplyExcludeFilter(
        AllColumnsNode node,
        List<ISchemaColumn> columns,
        TextSpan span)
    {
        if (node.ExcludeColumns is not { Length: > 0 })
            return columns;

        var columnNames = new HashSet<string>(columns.Select(c => c.ColumnName), StringComparer.OrdinalIgnoreCase);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var excl in node.ExcludeColumns)
        {
            if (!seen.Add(excl))
                throw new StarModifierValidationException(
                    $"Duplicate column '{excl}' in EXCLUDE list.",
                    DiagnosticCode.MQ3046_StarExcludeDuplicateColumn,
                    span);

            if (!columnNames.Contains(excl))
                throw new StarModifierValidationException(
                    $"EXCLUDE references non-existent column '{excl}'.",
                    DiagnosticCode.MQ3041_StarExcludeColumnNotFound,
                    span);
        }

        var excludeSet = new HashSet<string>(node.ExcludeColumns, StringComparer.OrdinalIgnoreCase);
        var surviving = columns.Where(c => !excludeSet.Contains(c.ColumnName)).ToList();

        if (surviving.Count == 0)
            throw new StarModifierValidationException(
                "EXCLUDE would remove all columns from the star expansion.",
                DiagnosticCode.MQ3043_StarExcludeRemovesAllColumns,
                span);

        return surviving;
    }

    private static List<(ISchemaColumn Column, Node? ReplacementExpression)> ApplyReplaceSubstitution(
        AllColumnsNode node,
        List<ISchemaColumn> surviving,
        List<ISchemaColumn> eligibleColumns,
        Node[]? inferredReplaceExpressions,
        TextSpan span)
    {
        var result = surviving.Select(c => (Column: c, ReplacementExpression: (Node?)null)).ToList();

        if (node.ReplaceItems is not { Length: > 0 })
            return result;

        var survivingNames = new HashSet<string>(surviving.Select(c => c.ColumnName), StringComparer.OrdinalIgnoreCase);
        var excludeSet = node.ExcludeColumns != null
            ? new HashSet<string>(node.ExcludeColumns, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < node.ReplaceItems.Length; i++)
        {
            var targetColumn = node.ReplaceItems[i].ColumnName;
            if (inferredReplaceExpressions == null)
                throw new InvalidOperationException("Star REPLACE items require inferred replacement expressions.");

            if (!seen.Add(targetColumn))
                throw new StarModifierValidationException(
                    $"Duplicate column '{targetColumn}' in REPLACE list.",
                    DiagnosticCode.MQ3047_StarReplaceDuplicateColumn,
                    span);

            if (excludeSet.Contains(targetColumn))
                throw new StarModifierValidationException(
                    $"Column '{targetColumn}' appears in both EXCLUDE and REPLACE.",
                    DiagnosticCode.MQ3044_StarColumnInBothExcludeAndReplace,
                    span);

            if (!survivingNames.Contains(targetColumn))
            {
                var eligibleNames = new HashSet<string>(eligibleColumns.Select(c => c.ColumnName), StringComparer.OrdinalIgnoreCase);
                var wasRemoved = eligibleNames.Contains(targetColumn);
                var code = wasRemoved
                    ? DiagnosticCode.MQ3048_StarReplaceTargetsRemovedColumn
                    : DiagnosticCode.MQ3042_StarReplaceColumnNotFound;
                var reason = wasRemoved
                    ? "was removed by LIKE filter or EXCLUDE"
                    : "does not exist in the table";
                throw new StarModifierValidationException(
                    $"REPLACE targets column '{targetColumn}' which {reason}.",
                    code,
                    span);
            }

            var replaceExpr = inferredReplaceExpressions[i];
            var idx = result.FindIndex(e =>
                string.Equals(e.Column.ColumnName, targetColumn, StringComparison.OrdinalIgnoreCase));
            result[idx] = (result[idx].Column, replaceExpr);
        }

        return result;
    }

    private static Func<string, bool> CreateLikeColumnMatcher(string pattern)
    {
        var sb = new System.Text.StringBuilder("^");
        foreach (var ch in pattern)
        {
            switch (ch)
            {
                case '%':
                    sb.Append(".*");
                    break;
                case '_':
                    sb.Append('.');
                    break;
                default:
                    sb.Append(System.Text.RegularExpressions.Regex.Escape(ch.ToString()));
                    break;
            }
        }
        sb.Append('$');

        var regex = new System.Text.RegularExpressions.Regex(
            sb.ToString(),
            System.Text.RegularExpressions.RegexOptions.IgnoreCase |
            System.Text.RegularExpressions.RegexOptions.Compiled);
        return regex.IsMatch;
    }
}
