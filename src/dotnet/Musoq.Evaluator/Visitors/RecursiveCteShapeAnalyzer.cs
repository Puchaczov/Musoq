using System.Collections.Generic;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public sealed class RecursiveCteShapeAnalyzer
{
    /// <summary>
    ///     Validates the parser-produced tree before semantic normalization.
    /// </summary>
    public IReadOnlyDictionary<string, RecursiveCteShapeDescriptor> AnalyzeRawSyntax(CteExpressionNode cte) =>
        AnalyzeCore(cte);

    /// <summary>
    ///     Classifies the semantically normalized tree for anchor-first binding.
    ///     It intentionally uses the same recursive shape rules as raw syntax validation.
    /// </summary>
    public IReadOnlyDictionary<string, RecursiveCteShapeDescriptor> AnalyzeBoundSyntax(CteExpressionNode cte) =>
        AnalyzeCore(cte);

    /// <summary>
    ///     Retained for callers that do not distinguish validation stages.
    /// </summary>
    public IReadOnlyDictionary<string, RecursiveCteShapeDescriptor> Analyze(CteExpressionNode cte)
        => AnalyzeRawSyntax(cte);

    private static IReadOnlyDictionary<string, RecursiveCteShapeDescriptor> AnalyzeCore(CteExpressionNode cte)
    {
        ArgumentNullException.ThrowIfNull(cte);

        var names = Array.ConvertAll(cte.InnerExpression, static definition => definition.Name);
        var indexes = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var index = 0; index < names.Length; index++)
            indexes[names[index]] = index;

        var descriptors = new Dictionary<string, RecursiveCteShapeDescriptor>(StringComparer.Ordinal);
        for (var index = 0; index < cte.InnerExpression.Length; index++)
        {
            var definition = cte.InnerExpression[index];
            var references = CountReferences(definition.Value, indexes.Keys);

            foreach (var reference in references.Keys)
            {
                if (!cte.IsRecursive || indexes[reference] <= index)
                    continue;

                Throw(
                    DiagnosticCode.MQ3074_InvalidRecursiveCteReference,
                    $"CTE '{definition.Name}' references forward CTE '{reference}'.",
                    FindFirstReference(definition.Value, reference) ?? definition,
                    definition);
            }

            var selfReferenceCount = references.GetValueOrDefault(definition.Name);
            if (selfReferenceCount == 0)
                continue;

            if (!cte.IsRecursive)
            {
                Throw(
                    DiagnosticCode.MQ3072_RecursiveCteRequiresKeyword,
                    ErrorCatalog.GetMessage(
                        DiagnosticCode.MQ3072_RecursiveCteRequiresKeyword,
                        definition.Name),
                    FindFirstReference(definition.Value, definition.Name) ?? definition,
                    definition);
            }

            descriptors.Add(definition.Name, AnalyzeDefinition(definition));
        }

        return descriptors;
    }

    private static RecursiveCteShapeDescriptor AnalyzeDefinition(CteInnerExpressionNode definition)
    {
        if (definition.Value is not UnionNode and not UnionAllNode)
        {
            Throw(
                DiagnosticCode.MQ3073_InvalidRecursiveCteShape,
                $"CTE '{definition.Name}' must have one top-level UNION boundary.",
                definition);
        }

        var boundary = (SetOperatorNode)definition.Value;
        if (boundary.Left is not QueryNode anchor || boundary.Right is not QueryNode recursiveMember)
        {
            Throw(
                DiagnosticCode.MQ3073_InvalidRecursiveCteShape,
                $"CTE '{definition.Name}' must have exactly one anchor query and one recursive member.",
                boundary);
            throw new InvalidOperationException("Unreachable recursive CTE shape validation path.");
        }

        var anchorReferences = ScanReferences(anchor, definition.Name);
        if (anchorReferences.Count > 0)
        {
            Throw(
                DiagnosticCode.MQ3074_InvalidRecursiveCteReference,
                $"The anchor of recursive CTE '{definition.Name}' cannot reference itself.",
                anchorReferences.FirstReference ?? anchor,
                anchor);
        }

        var recursiveReferences = ScanReferences(recursiveMember, definition.Name);
        if (recursiveReferences.Count != 1)
        {
            Throw(
                DiagnosticCode.MQ3074_InvalidRecursiveCteReference,
                $"The recursive member of CTE '{definition.Name}' must reference itself exactly once; found {recursiveReferences.Count}.",
                recursiveReferences.FirstReference ?? recursiveMember,
                recursiveMember);
        }

        if (recursiveReferences.HasNestedReference)
        {
            Throw(
                DiagnosticCode.MQ3074_InvalidRecursiveCteReference,
                $"The self-reference of recursive CTE '{definition.Name}' cannot appear in a nested query.",
                recursiveReferences.FirstReference ?? recursiveMember,
                recursiveMember);
        }

        if (boundary is UnionAllNode && boundary.Keys.Length > 0)
        {
            ThrowUnsupported("UNION ALL (keys)", boundary);
        }

        ValidateRecursiveMemberOperators(recursiveMember);

        var unionKind = boundary switch
        {
            UnionAllNode => RecursiveCteUnionKind.All,
            UnionNode when boundary.Keys.Length == 0 => RecursiveCteUnionKind.FullRow,
            UnionNode => RecursiveCteUnionKind.Keyed,
            _ => throw new InvalidOperationException("Unsupported recursive CTE union boundary.")
        };

        return new RecursiveCteShapeDescriptor(
            definition,
            anchor,
            recursiveMember,
            boundary,
            unionKind,
            boundary.Keys);
    }

    private static void ValidateRecursiveMemberOperators(QueryNode member)
    {
        if (member.Select.IsDistinct)
            ThrowUnsupported("DISTINCT", member.Select);
        if (member.GroupBy?.Having != null)
            ThrowUnsupported("HAVING", member.GroupBy.Having, member);
        if (member.GroupBy != null)
            ThrowUnsupported("GROUP BY", member.GroupBy);
        if (member.Window != null)
            ThrowUnsupported("WINDOW", member.Window, member);
        if (member.Qualify != null)
            ThrowUnsupported("QUALIFY", member.Qualify, member);
        if (member.OrderBy != null)
            ThrowUnsupported("ORDER BY", member.OrderBy);
        if (member.Skip != null || member.Take != null)
            ThrowUnsupported("pagination", member.Skip ?? (Node)member.Take!);

        ValidateMemberNode(member, member);
    }

    private static void ValidateMemberNode(Node node, QueryNode rootMember)
    {
        if (!ReferenceEquals(node, rootMember) && node is SetOperatorNode)
            ThrowUnsupported("nested set operation", node);

        if (!ReferenceEquals(node, rootMember) && node is QueryNode nestedQuery)
        {
            if (nestedQuery.GroupBy != null || nestedQuery.OrderBy != null || nestedQuery.Window != null ||
                nestedQuery.Qualify != null || nestedQuery.Skip != null || nestedQuery.Take != null ||
                nestedQuery.Select.IsDistinct)
            {
                ThrowUnsupported("nested query operator", nestedQuery);
            }
        }

        switch (node)
        {
            case JoinFromNode join when join.JoinType is not (JoinType.Inner or JoinType.Cross):
                ThrowUnsupported($"{join.JoinType} join", join);
                break;
            case JoinInMemoryWithSourceTableFromNode join
                when join.JoinType is not (JoinType.Inner or JoinType.Cross):
                ThrowUnsupported($"{join.JoinType} join", join);
                break;
            case UnpivotFromNode:
                ThrowUnsupported("UNPIVOT", node, rootMember);
                break;
        }

        foreach (var child in ParserNodeChildTraversal.EnumerateChildren(node))
            ValidateMemberNode(child, rootMember);
    }

    private static Dictionary<string, int> CountReferences(Node node, IEnumerable<string> knownNames)
    {
        var known = new HashSet<string>(knownNames, StringComparer.Ordinal);
        var references = new Dictionary<string, int>(StringComparer.Ordinal);
        CountReferences(node, known, references);
        return references;
    }

    private static void CountReferences(
        Node node,
        IReadOnlySet<string> knownNames,
        IDictionary<string, int> references)
    {
        var name = GetReferencedCteName(node);
        if (name != null && knownNames.Contains(name))
        {
            references.TryGetValue(name, out var count);
            references[name] = count + 1;
        }

        foreach (var child in ParserNodeChildTraversal.EnumerateChildren(node))
            CountReferences(child, knownNames, references);
    }

    private static ReferenceScan ScanReferences(Node node, string cteName)
    {
        var scan = new ReferenceScan();
        ScanReferences(node, cteName, scan, queryDepth: 0, isRoot: true);
        return scan;
    }

    private static void ScanReferences(
        Node node,
        string cteName,
        ReferenceScan scan,
        int queryDepth,
        bool isRoot)
    {
        if (!isRoot && node is QueryNode)
            queryDepth++;

        if (string.Equals(GetReferencedCteName(node), cteName, StringComparison.Ordinal))
        {
            scan.Count++;
            scan.HasNestedReference |= queryDepth > 0;
            scan.FirstReference ??= node;
        }

        foreach (var child in ParserNodeChildTraversal.EnumerateChildren(node))
            ScanReferences(child, cteName, scan, queryDepth, isRoot: false);
    }

    private static string? GetReferencedCteName(Node node)
    {
        return node switch
        {
            InMemoryTableFromNode source => source.VariableName,
            JoinInMemoryWithSourceTableFromNode join => join.InMemoryTableAlias,
            ApplyInMemoryWithSourceTableFromNode apply => apply.InMemoryTableAlias,
            _ => null
        };
    }

    private static Node? FindFirstReference(Node node, string cteName)
    {
        if (string.Equals(GetReferencedCteName(node), cteName, StringComparison.Ordinal))
            return node;

        foreach (var child in ParserNodeChildTraversal.EnumerateChildren(node))
        {
            var reference = FindFirstReference(child, cteName);
            if (reference != null)
                return reference;
        }

        return null;
    }

    private static void ThrowUnsupported(string operatorName, Node node, Node? fallback = null)
    {
        Throw(
            DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator,
            ErrorCatalog.GetMessage(
                DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator,
                operatorName),
            node,
            fallback);
    }

    private static void Throw(DiagnosticCode code, string message, Node node, Node? fallback = null)
    {
        var span = FindFirstNonEmptySpan(node);
        if (span.IsEmpty && fallback != null)
            span = FindFirstNonEmptySpan(fallback);

        throw new RecursiveCteValidationException(code, message, span);
    }

    private static TextSpan FindFirstNonEmptySpan(Node node)
    {
        var span = node.SpanOrEmpty();
        if (!span.IsEmpty)
            return span;

        foreach (var child in ParserNodeChildTraversal.EnumerateChildren(node))
        {
            span = FindFirstNonEmptySpan(child);
            if (!span.IsEmpty)
                return span;
        }

        return TextSpan.Empty;
    }

    private sealed class ReferenceScan
    {
        public int Count { get; set; }

        public bool HasNestedReference { get; set; }

        public Node? FirstReference { get; set; }
    }
}
