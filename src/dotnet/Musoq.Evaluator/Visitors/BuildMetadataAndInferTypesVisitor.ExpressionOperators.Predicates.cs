using System.Collections.Generic;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Diagnostics;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Utils.Symbols;
using static Musoq.Evaluator.Visitors.BinaryOperatorTypeRules;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(NotNode node)
    {
        var operand = SafePop(Nodes, VisitorOperationNames.VisitNotNode);
        if (operand is InNode { Right: ArgsListNode { Args.Length: 0 } })
        {
            if (TryReportSemanticError<NotSupportedException>(
                    DiagnosticCode.MQ2030_UnsupportedSyntax,
                    "NOT IN with an empty list is not supported.",
                    node))
            {
                Nodes.Push(new NotNode(operand));
                return;
            }
        }

        ValidateBooleanOperand(operand, "NOT", node);
        Nodes.Push(new NotNode(operand));
    }

    public override void Visit(LikeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var right = SafePop(Nodes, "Visit(LikeNode) right");
        var left = SafePop(Nodes, "Visit(LikeNode) left");

        ValidatePatternOperand(left, "LIKE", node);
        ValidatePatternOperand(right, "LIKE", node);

        Nodes.Push(new LikeNode(left, right));
    }

    public override void Visit(RLikeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var right = SafePop(Nodes, "Visit(RLikeNode) right");
        var left = SafePop(Nodes, "Visit(RLikeNode) left");

        ValidatePatternOperand(left, "RLIKE", node);
        ValidatePatternOperand(right, "RLIKE", node);

        Nodes.Push(new RLikeNode(left, right));
    }

    public override void Visit(InNode node)
    {
        var right = SafePop(Nodes, VisitorOperationNames.VisitInNodeRight);
        var left = SafePop(Nodes, VisitorOperationNames.VisitInNodeLeft);
        var args = (ArgsListNode)right;

        ValidateCollectionPredicateItems(left, args, node);

        Nodes.Push(new InNode(left, args));
    }

    public override void Visit(CollectionInNode node)
    {
        var collection = SafePop(Nodes, "Visit(CollectionInNode).Collection");
        var left = SafePop(Nodes, "Visit(CollectionInNode).Expression");

        ValidateCollectionParameterPredicate(left, collection, node);

        Nodes.Push(new CollectionInNode(left, collection));
    }

    public override void Visit(BetweenNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var max = SafePop(Nodes, "Visit(BetweenNode).Max");
        var min = SafePop(Nodes, "Visit(BetweenNode).Min");
        var expression = SafePop(Nodes, "Visit(BetweenNode).Expression");

        ValidateBinaryOperatorOperands(expression, min, BinaryOperatorKind.Relational, node);
        ValidateBinaryOperatorOperands(expression, max, BinaryOperatorKind.Relational, node);

        Nodes.Push(new BetweenNode(expression, min, max));
    }

    public override void Visit(ContainsNode node)
    {
        var right = SafePop(Nodes, VisitorOperationNames.VisitContainsNodeRight);
        var left = SafePop(Nodes, VisitorOperationNames.VisitContainsNodeLeft);
        var args = (ArgsListNode)right;

        ValidateCollectionPredicateItems(left, args, node);

        Nodes.Push(new ContainsNode(left, args));
    }

    public override void Visit(IsNullNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var operand = SafePop(Nodes, VisitorOperationNames.VisitIsNullNode);
        Nodes.Push(new IsNullNode(operand, node.IsNegated));
    }

    public override void Visit(RowPresenceNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (node.Expression is not IdentifierNode aliasNode)
        {
            const string message =
                "Row presence predicates require a source alias that can be absent because of an outer join or OUTER APPLY.";
            if (TryReportSemanticError<InvalidOperationException>(
                    DiagnosticCode.MQ3007_InvalidOperandTypes,
                    message,
                    node))
            {
                Nodes.Push(node);
                return;
            }

            throw new InvalidOperationException(message);
        }

        var alias = aliasNode.Name;
        var hasTableSymbol = TryGetCurrentTableSymbol(out var tableSymbol);
        if (!hasTableSymbol ||
            tableSymbol == null ||
            !tableSymbol.ContainsAlias(alias))
        {
            var availableAliases = tableSymbol?.CompoundTables ?? [];
            if (TryReportUnknownAlias(alias, availableAliases, node))
            {
                Nodes.Push(node);
                return;
            }

            var span = node.SpanOrEmpty();
            throw new VisitorException(
                VisitorName,
                VisitorOperationNames.VisitRowPresenceNode,
                $"Unknown alias '{alias}'.",
                DiagnosticCode.MQ3015_UnknownAlias,
                span);
        }

        if (!tableSymbol.CanAliasBeMissing(alias))
        {
            var message =
                $"Row presence predicates require an alias that can be absent because of LEFT, RIGHT, FULL, ASOF LEFT JOIN, or OUTER APPLY. Alias '{alias}' is always present in this scope.";
            if (TryReportSemanticError<InvalidOperationException>(
                    DiagnosticCode.MQ3007_InvalidOperandTypes,
                    message,
                    node))
            {
                Nodes.Push(node);
                return;
            }

            throw new InvalidOperationException(message);
        }

        Nodes.Push(new RowPresenceNode(aliasNode, node.IsPresent));
    }

    private bool TryGetCurrentTableSymbol(out TableSymbol? tableSymbol)
    {
        var candidates = new List<string>();

        if (_sourceBinding.CurrentScope.ContainsAttribute(MetaAttributes.ProcessedQueryId))
            candidates.Add(_sourceBinding.CurrentScope[MetaAttributes.ProcessedQueryId]);

        if (!string.IsNullOrWhiteSpace(_sourceBinding.Identifier))
            candidates.Add(_sourceBinding.Identifier);

        var scope = _sourceBinding.CurrentScope;
        while (scope != null)
        {
            foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (scope.ScopeSymbolTable.TryGetSymbol<TableSymbol>(candidate, out tableSymbol))
                    return true;
            }

            scope = scope.Parent;
        }

        tableSymbol = null;
        return false;
    }

    private void ValidateCollectionParameterPredicate(Node left, Node collection, Node errorContextNode)
    {
        if (collection is not ParameterReferenceNode parameter ||
            !TryGetCollectionElementType(collection.ReturnType, out var elementType))
        {
            var invalidCollectionMessage = collection is ParameterReferenceNode parameterReference
                ? $"IN ${parameterReference.Name} requires a one-dimensional array script parameter."
                : "IN $param requires a one-dimensional array script parameter.";

            if (TryReportSemanticError<InvalidOperationException>(
                    DiagnosticCode.MQ3007_InvalidOperandTypes,
                    invalidCollectionMessage,
                    errorContextNode))
                return;

            throw new InvalidOperationException(invalidCollectionMessage);
        }

        var leftType = NormalizeOperandType(left.ReturnType);
        var normalizedElementType = NormalizeOperandType(elementType);
        if (CanSkipStaticTypeValidation(leftType) ||
            CanSkipStaticTypeValidation(normalizedElementType) ||
            leftType == normalizedElementType)
            return;

        var message =
            $"Type mismatch: cannot compare expression of type '{FormatTypeName(leftType)}' with script parameter '${parameter.Name}' element type '{FormatTypeName(normalizedElementType)}'. Collection parameter membership requires matching element types; use an explicit conversion if needed.";

        if (TryReportTypeMismatch(message, errorContextNode))
            return;

        throw new TypeMismatchException(leftType, normalizedElementType, errorContextNode.SpanOrEmpty());
    }

    private static bool TryGetCollectionElementType(Type? collectionType, out Type elementType)
    {
        if (collectionType is { IsArray: true } && collectionType.GetArrayRank() == 1)
        {
            elementType = collectionType.GetElementType()!;
            return true;
        }

        elementType = typeof(object);
        return false;
    }
}
