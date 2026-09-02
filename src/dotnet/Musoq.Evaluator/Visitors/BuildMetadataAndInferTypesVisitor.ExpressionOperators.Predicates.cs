using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Diagnostics;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Utils.Symbols;
using static Musoq.Evaluator.Visitors.BinaryOperatorTypeRules;
using static Musoq.Evaluator.Visitors.SemanticExpressionDiagnosticFacts;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(NotNode node)
    {
        var operand = PopSemanticNode(VisitorOperationNames.VisitNotNode);
        if (operand is InNode { Right: ArgsListNode { Args.Length: 0 } })
        {
            if (TryReportSemanticError<NotSupportedException>(
                    DiagnosticCode.MQ2030_UnsupportedSyntax,
                    "NOT IN with an empty list is not supported.",
                    node))
            {
                PushSemanticNode(new NotNode(operand));
                return;
            }
        }

        ValidateBooleanOperand(operand, "NOT", node);
        PushSemanticNode(new NotNode(operand));
    }

    public override void Visit(LikeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var right = PopSemanticNode("Visit(LikeNode) right");
        var left = PopSemanticNode("Visit(LikeNode) left");

        if (TryRejectUnsupportedEnumOperator("LIKE", node, left, right))
        {
            PushSemanticNode(new LikeNode(left, right));
            return;
        }

        ValidatePatternOperand(left, "LIKE", node);
        ValidatePatternOperand(right, "LIKE", node);

        PushSemanticNode(new LikeNode(left, right));
    }

    public override void Visit(RLikeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var right = PopSemanticNode("Visit(RLikeNode) right");
        var left = PopSemanticNode("Visit(RLikeNode) left");

        if (TryRejectUnsupportedEnumOperator("RLIKE", node, left, right))
        {
            PushSemanticNode(new RLikeNode(left, right));
            return;
        }

        ValidatePatternOperand(left, "RLIKE", node);
        ValidatePatternOperand(right, "RLIKE", node);
        ValidateConstantRegex(right);

        PushSemanticNode(new RLikeNode(left, right));
    }

    private void ValidateConstantRegex(Node pattern)
    {
        if (pattern is not ConstantValueNode { ObjValue: string regexPattern })
            return;

        try
        {
            _ = new Regex(regexPattern, RegexOptions.Compiled, RuntimeCacheOptions.DefaultRegexTimeout);
        }
        catch (ArgumentException exception)
        {
            var message = $"Invalid constant regex pattern '{regexPattern}': {exception.Message}";
            if (TryReportSemanticError<ArgumentException>(DiagnosticCode.MQ3094_InvalidConstantRegex, message, pattern))
                return;

            throw;
        }
    }

    public override void Visit(InNode node)
    {
        var right = PopSemanticNode(VisitorOperationNames.VisitInNodeRight);
        var left = PopSemanticNode(VisitorOperationNames.VisitInNodeLeft);
        var args = (ArgsListNode)right;

        if (TryBindEnumCollectionPredicate(left, args, node, out var enumArgs))
        {
            PushSemanticNode(new InNode(left, enumArgs));
            return;
        }

        ValidateCollectionPredicateItems(left, args, node);

        PushSemanticNode(new InNode(left, args));
    }

    public override void Visit(CollectionInNode node)
    {
        var collection = PopSemanticNode("Visit(CollectionInNode).Collection");
        var left = PopSemanticNode("Visit(CollectionInNode).Expression");

        if (TryGetEnumExpressionType(left, out var enumType))
        {
            ReportEnumSemanticError(
                DiagnosticCode.MQ3112_UnsupportedEnumScriptParameter,
                $"Enum script parameters are not supported for enum type '{enumType.DisplayName}'. Use exact quoted members directly in IN (...).",
                node);
            PushSemanticNode(new CollectionInNode(left, collection));
            return;
        }

        ValidateCollectionParameterPredicate(left, collection, node);

        PushSemanticNode(new CollectionInNode(left, collection));
    }

    public override void Visit(BetweenNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var max = PopSemanticNode("Visit(BetweenNode).Max");
        var min = PopSemanticNode("Visit(BetweenNode).Min");
        var expression = PopSemanticNode("Visit(BetweenNode).Expression");

        if (TryRejectUnsupportedEnumOperator("BETWEEN", node, expression, min, max))
        {
            PushSemanticNode(new BetweenNode(expression, min, max));
            return;
        }

        ValidateBinaryOperatorOperands(expression, min, BinaryOperatorKind.Relational, node);
        ValidateBinaryOperatorOperands(expression, max, BinaryOperatorKind.Relational, node);

        PushSemanticNode(new BetweenNode(expression, min, max));
    }

    public override void Visit(ContainsNode node)
    {
        var right = PopSemanticNode(VisitorOperationNames.VisitContainsNodeRight);
        var left = PopSemanticNode(VisitorOperationNames.VisitContainsNodeLeft);
        var args = (ArgsListNode)right;

        if (TryBindEnumCollectionPredicate(left, args, node, out var enumArgs))
        {
            PushSemanticNode(new ContainsNode(left, enumArgs));
            return;
        }

        ValidateCollectionPredicateItems(left, args, node);

        PushSemanticNode(new ContainsNode(left, args));
    }

    public override void Visit(IsNullNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var operand = PopSemanticNode(VisitorOperationNames.VisitIsNullNode);
        PushSemanticNode(new IsNullNode(operand, node.IsNegated));
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
                PushSemanticNode(node);
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
                PushSemanticNode(node);
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
                PushSemanticNode(node);
                return;
            }

            throw new InvalidOperationException(message);
        }

        PushSemanticNode(new RowPresenceNode(aliasNode, node.IsPresent)
            .WithSpan(node.Span)
            .WithFullSpan(node.FullSpan));
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
