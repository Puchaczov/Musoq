using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(JoinFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var tieBreakNode = node.TieBreak == null ? null : PopSemanticNode();
        if (tieBreakNode is not null && tieBreakNode is not FieldOrderedNode)
            throw VisitorException.CreateForProcessingFailure(
                nameof(BuildMetadataAndInferTypesVisitor),
                nameof(Visit),
                "ASOF JOIN tie-break expression did not produce an ordered field.");

        var tieBreak = tieBreakNode as FieldOrderedNode;
        var expression = PopSemanticNode();
        var joinedTableNode = PopSemanticNode();
        var sourceNode = PopSemanticNode();
        if (joinedTableNode is not FromNode joinedTable || sourceNode is not FromNode source)
            throw VisitorException.CreateForProcessingFailure(
                nameof(BuildMetadataAndInferTypesVisitor),
                nameof(Visit),
                "JOIN inputs did not produce source nodes.");

        if (node.JoinType is JoinType.AsOf or JoinType.AsOfLeft)
            ValidateAsOfJoinCondition(expression, source, joinedTable, tieBreak);

        var joinedFrom = new Parser.JoinFromNode(source, joinedTable, expression, node.JoinType, tieBreak);
        _sourceBinding.Identifier = joinedFrom.Alias;
        PushSemanticNode(joinedFrom);
    }

    private void ValidateAsOfJoinCondition(Node expression, FromNode source, FromNode joinedTable, FieldOrderedNode? tieBreak)
    {
        if (ContainsOrNode(expression))
            throw new VisitorException(
                nameof(BuildMetadataAndInferTypesVisitor),
                "ASOF JOIN validation",
                "ASOF JOIN ON clause does not support OR.",
                DiagnosticCode.MQ3038_AsOfJoinOrNotSupported,
                expression.Span);

        var (inequalities, _) = CollectConditions(expression);

        if (inequalities.Count == 0)
            throw new VisitorException(
                nameof(BuildMetadataAndInferTypesVisitor),
                "ASOF JOIN validation",
                "ASOF JOIN requires at least one inequality condition (>=, >, <=, <).",
                DiagnosticCode.MQ3036_AsOfJoinMissingInequality,
                expression.Span);

        if (inequalities.Count > 1)
            throw new VisitorException(
                nameof(BuildMetadataAndInferTypesVisitor),
                "ASOF JOIN validation",
                $"ASOF JOIN supports exactly one inequality condition. Found {inequalities.Count}.",
                DiagnosticCode.MQ3037_AsOfJoinMultipleInequalities,
                expression.Span);

        var inequality = inequalities[0];
        var leftAliases = CollectFromNodeAliases(source);
        var rightAliases = CollectFromNodeAliases(joinedTable);
        ValidateInequalityReferencesBothSides(inequality, leftAliases, rightAliases);
        ValidateInequalityColumnIsOrderable(inequality);
        ValidateTieBreakReferencesRightSide(tieBreak, leftAliases, rightAliases);
        ValidateTieBreakColumnIsOrderable(tieBreak);
    }

    private static bool ContainsOrNode(Node node)
    {
        if (node is OrNode)
            return true;

        if (node is AndNode and)
            return ContainsOrNode(and.Left) || ContainsOrNode(and.Right);

        return false;
    }

    private static (List<BinaryNode> Inequalities, int EqualityCount) CollectConditions(Node node)
    {
        var inequalities = new List<BinaryNode>();
        var equalityCount = 0;
        var stack = new Stack<Node>();
        stack.Push(node);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (current is AndNode and)
            {
                stack.Push(and.Right);
                stack.Push(and.Left);
                continue;
            }

            if (current is BinaryNode binary)
            {
                if (IsInequalityNode(binary))
                    inequalities.Add(binary);
                else
                    equalityCount++;
            }
        }

        return (inequalities, equalityCount);
    }

    private static bool IsInequalityNode(BinaryNode node)
    {
        return node is GreaterNode or LessNode or GreaterOrEqualNode or LessOrEqualNode;
    }

    private void ValidateInequalityReferencesBothSides(BinaryNode inequality, HashSet<string> leftAliases, HashSet<string> rightAliases)
    {
        var columnAliases = ExtractColumnAliases(inequality.Left);
        columnAliases.UnionWith(ExtractColumnAliases(inequality.Right));

        var referencesLeft = columnAliases.Overlaps(leftAliases);
        var referencesRight = columnAliases.Overlaps(rightAliases);

        if (!referencesLeft || !referencesRight)
            throw new VisitorException(
                nameof(BuildMetadataAndInferTypesVisitor),
                "ASOF JOIN validation",
                "ASOF JOIN inequality must reference columns from both sides.",
                DiagnosticCode.MQ3039_AsOfJoinInequalityMustReferenceBothSides,
                inequality.Span);
    }

    private static void ValidateTieBreakReferencesRightSide(
        FieldOrderedNode? tieBreak,
        HashSet<string> leftAliases,
        HashSet<string> rightAliases)
    {
        if (tieBreak == null)
            return;

        var columnAliases = ExtractColumnAliases(tieBreak.Expression);
        if (columnAliases.Count == 0)
            return;

        if (columnAliases.Overlaps(leftAliases) || columnAliases.Any(alias => !rightAliases.Contains(alias)))
        {
            throw new VisitorException(
                nameof(BuildMetadataAndInferTypesVisitor),
                "ASOF JOIN validation",
                "ASOF JOIN TIE BREAK BY expression must reference only right-side columns.",
                DiagnosticCode.MQ3039_AsOfJoinInequalityMustReferenceBothSides,
                tieBreak.SpanOrEmpty());
        }
    }

    private static HashSet<string> CollectFromNodeAliases(FromNode node)
    {
        var aliases = new HashSet<string>();
        CollectFromNodeAliasesRecursive(node, aliases);
        return aliases;
    }

    private static void CollectFromNodeAliasesRecursive(FromNode node, HashSet<string> aliases)
    {
        if (node == null) return;

        aliases.Add(node.Alias);

        if (node is JoinFromNode joinNode)
        {
            CollectFromNodeAliasesRecursive(joinNode.Source, aliases);
            CollectFromNodeAliasesRecursive(joinNode.With, aliases);
        }
    }

    private static HashSet<string> ExtractColumnAliases(Node node)
    {
        var aliases = new HashSet<string>();
        var stack = new Stack<Node>();
        stack.Push(node);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (current is AccessColumnNode col)
            {
                aliases.Add(col.Alias);
                continue;
            }

            foreach (var child in ParserNodeChildTraversal.EnumerateChildren(current))
                stack.Push(child);
        }

        return aliases;
    }

    private static void ValidateInequalityColumnIsOrderable(BinaryNode inequality)
    {
        ThrowIfNotOrderable(inequality.Left.ReturnType);
        ThrowIfNotOrderable(inequality.Right.ReturnType);
    }

    private static void ValidateTieBreakColumnIsOrderable(FieldOrderedNode? tieBreak)
    {
        if (tieBreak == null)
            return;

        ThrowIfNotOrderable(tieBreak.Expression.ReturnType);
    }

    private static void ThrowIfNotOrderable(Type? columnType)
    {
        if (columnType == null)
            throw new VisitorException(
                nameof(BuildMetadataAndInferTypesVisitor),
                "ASOF JOIN validation",
                "ASOF JOIN inequality column type could not be inferred.",
                DiagnosticCode.MQ3040_AsOfJoinInequalityColumnNotOrderable,
                TextSpan.Empty);

        var underlying = Nullable.GetUnderlyingType(columnType) ?? columnType;

        if (!IsOrderableType(underlying))
            throw new VisitorException(
                nameof(BuildMetadataAndInferTypesVisitor),
                "ASOF JOIN validation",
                $"ASOF JOIN inequality column type '{underlying.Name}' is not orderable.",
                DiagnosticCode.MQ3040_AsOfJoinInequalityColumnNotOrderable,
                TextSpan.Empty);
    }

    private static bool IsOrderableType(Type type)
    {
        return typeof(IComparable).IsAssignableFrom(type) ||
               type.IsPrimitive ||
               type == typeof(string) ||
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(TimeSpan) ||
               type == typeof(decimal);
    }
}
