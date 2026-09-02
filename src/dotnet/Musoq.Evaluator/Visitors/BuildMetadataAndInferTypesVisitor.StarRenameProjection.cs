using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private static List<StarProjectedColumn> CreateSingleTableStarProjectedColumns(
        TableSymbol tableSymbol,
        string generatedColumnIdentifier,
        List<(ISchemaColumn Column, Node? ReplacementExpression)> filteredColumns)
    {
        return filteredColumns
            .Select(entry =>
                new StarProjectedColumn(
                    generatedColumnIdentifier,
                    entry.Column,
                    entry.ReplacementExpression,
                    tableSymbol.HasAlias
                        ? $"{generatedColumnIdentifier}.{entry.Column.ColumnName}"
                        : entry.Column.ColumnName))
            .ToList();
    }

    private static List<StarProjectedColumn> CreateCompoundTableStarProjectedColumns(
        List<(string TableIdentifier, ISchemaColumn Column)> allEligible,
        List<(ISchemaColumn Column, Node? ReplacementExpression)> filtered)
    {
        var projected = new List<StarProjectedColumn>(filtered.Count);
        foreach (var entry in filtered)
        {
            var originalIndex = FindOriginalIndex(allEligible, entry.Column);
            var tableIdentifier = allEligible[originalIndex].TableIdentifier;
            projected.Add(new StarProjectedColumn(
                tableIdentifier,
                entry.Column,
                entry.ReplacementExpression,
                $"{tableIdentifier}.{entry.Column.ColumnName}"));
        }

        return projected;
    }

    private static void ApplyRenameSubstitution(
        AllColumnsNode node,
        List<StarProjectedColumn> projectedColumns,
        TextSpan span)
    {
        if (node.RenameItems is not { Length: > 0 })
            return;

        var sourceSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var renamedIndexes = new Dictionary<int, string>();

        foreach (var rename in node.RenameItems)
        {
            if (!sourceSeen.Add(rename.SourceName))
                throw new StarModifierValidationException(
                    $"Duplicate source column '{rename.SourceName}' in RENAME list.",
                    DiagnosticCode.MQ3068_StarRenameDuplicateSource,
                    span);

            var sourceIndex = FindRenameSourceIndex(projectedColumns, rename.SourceName);
            if (sourceIndex < 0)
                throw new StarModifierValidationException(
                    $"RENAME references non-existent output column '{rename.SourceName}'.",
                    DiagnosticCode.MQ3070_StarRenameColumnNotFound,
                    span);

            renamedIndexes[sourceIndex] = rename.TargetName;
        }

        ApplyRenameTargets(projectedColumns, renamedIndexes, span);
    }

    private static void ApplyRenameTargets(
        List<StarProjectedColumn> projectedColumns,
        Dictionary<int, string> renamedIndexes,
        TextSpan span)
    {
        var finalNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var isSingleTableExpansion = projectedColumns
            .Select(static column => column.TableIdentifier)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == 1;

        for (var i = 0; i < projectedColumns.Count; i++)
        {
            var finalName = renamedIndexes.TryGetValue(i, out var renamed)
                ? renamed
                : isSingleTableExpansion
                    ? projectedColumns[i].Column.ColumnName
                    : projectedColumns[i].OutputName;

            if (!finalNames.Add(finalName))
                throw new StarModifierValidationException(
                    $"RENAME would produce duplicate output column '{finalName}'.",
                    DiagnosticCode.MQ3069_StarRenameDuplicateTarget,
                    span);
        }

        foreach (var (index, targetName) in renamedIndexes)
            projectedColumns[index].OutputName = targetName;
    }

    private static int FindRenameSourceIndex(
        List<StarProjectedColumn> projectedColumns,
        string sourceName)
    {
        var exactIndex = FindRenameSourceIndex(
            projectedColumns,
            sourceName,
            static column => column.OutputName);
        if (exactIndex >= 0)
            return exactIndex;

        return FindRenameSourceIndex(
            projectedColumns,
            sourceName,
            static column => column.Column.ColumnName);
    }

    private static int FindRenameSourceIndex(
        List<StarProjectedColumn> projectedColumns,
        string sourceName,
        Func<StarProjectedColumn, string> nameSelector)
    {
        var matchIndex = -1;
        for (var i = 0; i < projectedColumns.Count; i++)
        {
            if (!string.Equals(nameSelector(projectedColumns[i]), sourceName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (matchIndex >= 0)
                return -1;

            matchIndex = i;
        }

        return matchIndex;
    }

    private sealed class StarProjectedColumn(
        string tableIdentifier,
        ISchemaColumn column,
        Node? replacementExpression,
        string outputName)
    {
        public string TableIdentifier { get; } = tableIdentifier;

        public ISchemaColumn Column { get; } = column;

        public Node? ReplacementExpression { get; } = replacementExpression;

        public string OutputName { get; set; } = outputName;
    }
}
