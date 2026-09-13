using System.Collections.Generic;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal sealed class SemanticDiagnosticReporter(
    DiagnosticContext? diagnosticContext,
    IReadOnlyCollection<string>? generatedAliases = null)
{
    private readonly IReadOnlyCollection<string> _generatedAliases = generatedAliases ?? [];

    public bool TryReportTypeMismatch(string message, Node node)
    {
        if (diagnosticContext == null)
            return false;

        diagnosticContext.ReportError(DiagnosticCode.MQ3005_TypeMismatch, message, node);
        return true;
    }

    public bool TryReportException(Exception exception, Node? node)
    {
        if (diagnosticContext == null)
            return false;

        diagnosticContext.ReportException(exception, node?.Span);
        return true;
    }

    public bool TryReportInvalidExpressionType(FieldNode field, Type? invalidType, string context, Node? node)
    {
        if (diagnosticContext == null)
            return false;

        if (diagnosticContext.HasErrors)
            return true;

        diagnosticContext.ReportError(
            DiagnosticCode.MQ3027_InvalidExpressionType,
            $"Query output column '{field.FieldName}' has invalid type '{invalidType?.Name ?? "null"}' in {context}. Only primitive types are allowed in query outputs.",
            node);
        return true;
    }

    public bool TryReportInvalidExpressionType(string expressionDescription, Type? invalidType, string context, Node? node)
    {
        if (diagnosticContext == null)
            return false;

        if (diagnosticContext.HasErrors)
            return true;

        diagnosticContext.ReportError(
            DiagnosticCode.MQ3027_InvalidExpressionType,
            $"Expression '{expressionDescription}' has invalid type '{invalidType?.Name ?? "null"}' in {context}. Only primitive types are allowed in query expressions.",
            node);
        return true;
    }

    public bool TryReportNonAggregatedColumnInSelect(
        string columnName,
        IEnumerable<string> groupByColumns,
        IEnumerable<(Node Expression, TextSpan Span)> groupByFields,
        Node? node)
    {
        if (diagnosticContext == null)
            return false;

        // An invalid aggregate in GROUP BY owns the structural failure. The
        // non-aggregate projection error is only a dependent consequence of
        // the same malformed grouping shape.
        if (diagnosticContext.Diagnostics.Any(diagnostic =>
                diagnostic.Code == DiagnosticCode.MQ3092_AggregateInGroupBy))
            return true;

        // A projection with its own binding/type root cannot also be
        // validated as a sound grouping consumer. Keep the root diagnostic and
        // suppress only the derivative GROUP BY complaint inside that field.
        if (node != null && DiagnosticRecovery.HasErrorsWithin(diagnosticContext, node))
            return true;

        // An unresolved GROUP BY consumer owns the scope failure only for a
        // projection that depends on that same missing group key. Keep
        // independent sibling projection roots visible.
        var groupByFieldList = groupByFields.ToArray();
        if (HasDependentUnresolvedGroupByRoot(columnName, groupByFieldList, node))
            return true;

        var groupByColumnList = groupByColumns
            .Select(FormatGroupByColumn)
            .ToArray();
        var groupByList = groupByColumnList.Length > 0
            ? string.Join(", ", groupByColumnList)
            : "(none)";
        diagnosticContext.ReportError(
            DiagnosticCode.MQ3012_NonAggregateInSelect,
            $"Column '{columnName}' must appear in the GROUP BY clause or be used in an aggregate function. " +
            $"Current GROUP BY columns: {groupByList}.",
            node);
        return true;
    }

    private bool HasDependentUnresolvedGroupByRoot(
        string columnName,
        IReadOnlyList<(Node Expression, TextSpan Span)> groupByFields,
        Node? projectionNode)
    {
        var unresolvedGroupByDiagnostics = diagnosticContext!.Diagnostics
            .Where(static diagnostic =>
                diagnostic.Code is DiagnosticCode.MQ3001_UnknownColumn or DiagnosticCode.MQ3015_UnknownAlias)
            .Where(diagnostic => groupByFields.Any(field =>
                !field.Span.IsEmpty && field.Span.Overlaps(diagnostic.Span)))
            .ToArray();

        if (unresolvedGroupByDiagnostics.Length == 0)
            return false;

        foreach (var field in groupByFields)
        {
            var fieldDiagnostics = unresolvedGroupByDiagnostics
                .Where(diagnostic => !field.Span.IsEmpty && field.Span.Overlaps(diagnostic.Span));
            var terminalName = GetTerminalIdentifier(field.Expression.ToString());

            foreach (var diagnostic in fieldDiagnostics)
            {
                // An unknown alias has no reliable source identity. Its
                // dependent projection cascade is still suppressed by the
                // terminal column name, preserving the existing recovery
                // contract for missing query-scope aliases.
                if (diagnostic.Code == DiagnosticCode.MQ3015_UnknownAlias &&
                    string.Equals(terminalName, columnName, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (diagnostic.Code != DiagnosticCode.MQ3001_UnknownColumn)
                    continue;

                // A qualified unknown column retains its source alias. Do
                // not let a repair candidate such as "Country" suppress an
                // independent projection from another source alias.
                if (!HasCompatibleSourceIdentity(field.Expression, projectionNode))
                    continue;

                foreach (var key in new[] { "suggestion", "candidateColumns" })
                {
                    if (!diagnostic.Arguments.TryGetValue(key, out var candidates))
                        continue;

                    if (candidates.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                        .Any(candidate => string.Equals(candidate, columnName, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }
            }
        }

        return false;
    }

    private static bool HasCompatibleSourceIdentity(Node groupByExpression, Node? projectionNode)
    {
        var projectionExpression = projectionNode is FieldNode field
            ? field.Expression
            : projectionNode;
        var groupByQualifiers = GetExpressionQualifiers(groupByExpression);
        var projectionQualifiers = GetExpressionQualifiers(projectionExpression);

        // An unqualified expression cannot disprove a dependency. When both
        // sides carry source identity, require the complete qualifier sets to
        // agree so mixed-source projections remain independent.
        if (groupByQualifiers.Count == 0 || projectionQualifiers.Count == 0)
            return true;

        return groupByQualifiers.Count == projectionQualifiers.Count &&
               groupByQualifiers.All(projectionQualifiers.Contains);
    }

    private static HashSet<string> GetExpressionQualifiers(Node? expression)
    {
        var qualifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (expression == null)
            return qualifiers;

        var pending = new Stack<Node>();
        pending.Push(expression);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            switch (current)
            {
                case DotNode dot:
                {
                    var qualifier = GetRootIdentifier(dot.Root);
                    if (!string.IsNullOrWhiteSpace(qualifier))
                        qualifiers.Add(qualifier);
                    break;
                }
                case AccessColumnNode column when !string.IsNullOrWhiteSpace(column.Alias):
                    qualifiers.Add(column.Alias);
                    break;
            }

            foreach (var child in ParserNodeTraversalRegistry.EnumerateChildren(current))
                pending.Push(child);
        }

        return qualifiers;
    }

    private static string? GetRootIdentifier(Node node)
    {
        while (node is DotNode dot)
            node = dot.Root;

        return node switch
        {
            AccessColumnNode column when !string.IsNullOrWhiteSpace(column.Alias) => column.Alias,
            IdentifierNode identifier => identifier.Name,
            _ => null
        };
    }

    private static string? GetTerminalIdentifier(string expression)
    {
        var terminal = expression.Trim();
        var separator = terminal.LastIndexOf('.');
        if (separator >= 0)
            terminal = terminal[(separator + 1)..];

        return terminal.Length == 0 ? null : terminal.Trim('[', ']');
    }

    private string FormatGroupByColumn(string value)
    {
        foreach (var alias in _generatedAliases)
        {
            if (value.StartsWith(alias + ".", StringComparison.OrdinalIgnoreCase))
                return value[(alias.Length + 1)..];
        }

        return value;
    }
}
