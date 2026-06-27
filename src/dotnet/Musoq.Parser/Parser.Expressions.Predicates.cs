using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private Node ComposeEqualityOperators()
    {
        var node = ComposeArithmeticExpression(0);

        while (IsEqualityOperator(Current))
            switch (Current.TokenType)
            {
                case TokenType.GreaterEqual:
                    Consume(TokenType.GreaterEqual);
                    node = ComposeComparisonRight(node, TokenType.GreaterEqual);
                    break;
                case TokenType.Greater:
                    Consume(TokenType.Greater);
                    node = ComposeComparisonRight(node, TokenType.Greater);
                    break;
                case TokenType.LessEqual:
                    Consume(TokenType.LessEqual);
                    node = ComposeComparisonRight(node, TokenType.LessEqual);
                    break;
                case TokenType.Less:
                    Consume(TokenType.Less);
                    node = ComposeComparisonRight(node, TokenType.Less);
                    break;
                case TokenType.Equality:
                    Consume(TokenType.Equality);
                    node = ComposeComparisonRight(node, TokenType.Equality);
                    break;
                case TokenType.Diff:
                    Consume(TokenType.Diff);
                    node = ComposeComparisonRight(node, TokenType.Diff);
                    break;
                case TokenType.Not:
                    Consume(TokenType.Not);
                    node = new NotNode(node);
                    break;
                case TokenType.Like:
                case TokenType.NotLike:
                case TokenType.RLike:
                case TokenType.NotRLike:
                    node = ComposePatternPredicate(node, Current.TokenType);
                    break;
                case TokenType.Contains:
                    Consume(TokenType.Contains);
                    node = new ContainsNode(node, ComposeNonEmptyArgs("CONTAINS"));
                    break;
                case TokenType.Is:
                    Consume(TokenType.Is);
                    node = ComposeIsPredicate(node);
                    break;
                case TokenType.In:
                    Consume(TokenType.In);
                    node = ComposeInExpression(node);
                    break;
                case TokenType.NotIn:
                    Consume(TokenType.NotIn);
                    node = new NotNode(ComposeInExpression(node));
                    break;
                case TokenType.Between:
                    node = ComposeBetween(node);
                    break;
                default:
                    throw new NotSupportedException(
                        $"Unrecognized token for ComposeEqualityOperators(), the token was {Current.TokenType}");
            }

        return node;
    }

    private Node ComposePatternPredicate(Node left, TokenType operatorType)
    {
        Consume(operatorType);
        var right = ComposeBaseTypes();

        if (left is AccessMethodNode quantifier
            && string.IsNullOrEmpty(quantifier.Alias)
            && IsPredicateQuantifierName(quantifier.Name))
            return ComposePredicateQuantifier(quantifier, operatorType, right);

        return CreatePatternPredicate(left, operatorType, right);
    }

    private Node ComposeComparisonRight(Node left, TokenType operatorType)
        => IsQuantifiedSubqueryToken(Current.TokenType)
            ? ComposeQuantifiedSubquery(left, operatorType)
            : CreateComparisonPredicate(left, operatorType, ComposeEqualityOperators());


    private Node ComposePredicateQuantifier(AccessMethodNode quantifier, TokenType operatorType, Node right)
    {
        ThrowIfPredicateQuantifierHasNoArguments(quantifier, operatorType);
        ThrowIfPredicateQuantifierHasStarArgument(quantifier);

        var predicates = quantifier.Arguments.Args
            .Select(argument => CreatePatternPredicate(argument, operatorType, right))
            .ToArray();

        return FoldPredicateQuantifier(quantifier, predicates);
    }


    private void ThrowIfPredicateQuantifierHasNoArguments(AccessMethodNode quantifier, TokenType operatorType)
    {
        if (quantifier.Arguments.Args.Length != 0)
            return;

        throw new SyntaxException(
            $"{GetPredicateQuantifierDiagnosticName(quantifier)} requires at least one argument before {GetPatternOperatorText(operatorType)}.",
            _lexer.AlreadyResolvedQueryPart,
            DiagnosticCode.MQ2003_InvalidExpression,
            quantifier.Span);
    }


    private void ThrowIfPredicateQuantifierHasStarArgument(AccessMethodNode quantifier)
    {
        var starArgument = quantifier.Arguments.Args.FirstOrDefault(argument => argument is AllColumnsNode);
        if (starArgument == null)
            return;

        throw new SyntaxException(
            $"{GetPredicateQuantifierDiagnosticName(quantifier)} does not support star arguments; list columns or expressions explicitly.",
            _lexer.AlreadyResolvedQueryPart,
            DiagnosticCode.MQ2003_InvalidExpression,
            starArgument.Span);
    }


    private static Node FoldPredicateQuantifier(AccessMethodNode quantifier, Node[] predicates)
    {
        var predicate = predicates[0];

        for (var index = 1; index < predicates.Length; index += 1)
        {
            if (IsAnyPredicateQuantifier(quantifier))
            {
                predicate = new OrNode(predicate, predicates[index]);
                continue;
            }

            predicate = new AndNode(predicate, predicates[index]);
        }

        return predicate;
    }


    private static Node CreatePatternPredicate(Node left, TokenType operatorType, Node right)
    {
        return operatorType switch
        {
            TokenType.Like => new LikeNode(left, right),
            TokenType.NotLike => new NotNode(new LikeNode(left, right)),
            TokenType.RLike => new RLikeNode(left, right),
            TokenType.NotRLike => new NotNode(new RLikeNode(left, right)),
            _ => throw new NotSupportedException($"{operatorType} is not supported while parsing pattern predicate.")
        };
    }


    private static bool IsPredicateQuantifierName(string name)
    {
        return string.Equals(name, "any", StringComparison.OrdinalIgnoreCase)
               || string.Equals(name, "all", StringComparison.OrdinalIgnoreCase);
    }


    private static bool IsAnyPredicateQuantifier(AccessMethodNode quantifier)
    {
        return string.Equals(quantifier.Name, "any", StringComparison.OrdinalIgnoreCase);
    }


    private static string GetPredicateQuantifierDiagnosticName(AccessMethodNode quantifier)
    {
        return quantifier.Name.ToUpperInvariant();
    }


    private static string GetPatternOperatorText(TokenType operatorType)
    {
        return operatorType switch
        {
            TokenType.Like => "LIKE",
            TokenType.NotLike => "NOT LIKE",
            TokenType.RLike => "RLIKE",
            TokenType.NotRLike => "NOT RLIKE",
            _ => operatorType.ToString().ToUpperInvariant()
        };
    }


    private BetweenNode ComposeBetween(Node expression)
    {
        Consume(TokenType.Between);
        var min = ComposeArithmeticExpression(0);
        Consume(TokenType.And);
        var max = ComposeArithmeticExpression(0);
        return new BetweenNode(expression, min, max);
    }


    private Node ComposeInExpression(Node left)
    {
        if (Current.TokenType == TokenType.ParameterReference)
        {
            var parameter = ConsumeAndGetToken(TokenType.ParameterReference);
            return new CollectionInNode(left, new ParameterReferenceNode(parameter.Value, null, parameter.Span));
        }

        Consume(TokenType.LeftParenthesis);

        if (Current.TokenType == TokenType.Select || Current.TokenType == TokenType.From || Current.TokenType == TokenType.Pivot || Current.TokenType == TokenType.Unpivot)
        {
            Node subquery = ComposeSetOperators(1);

            Consume(TokenType.RightParenthesis);
            return new InQueryNode(left, subquery);
        }

        var args = new List<Node>();

        if (Current.TokenType != TokenType.RightParenthesis)
            do
            {
                if (Current.TokenType == TokenType.Comma)
                    Consume(Current.TokenType);

                args.Add(ComposeEqualityOperators());
            } while (Current.TokenType == TokenType.Comma);

        Consume(TokenType.RightParenthesis);

        return new InNode(left, new ArgsListNode(args.ToArray()));
    }

    private ExistsQueryNode ComposeExistsExpression()
    {
        Consume(TokenType.Exists);
        Consume(TokenType.LeftParenthesis);

        if (Current.TokenType != TokenType.Select && Current.TokenType != TokenType.From && Current.TokenType != TokenType.Pivot && Current.TokenType != TokenType.Unpivot)
            throw new SyntaxException(
                "EXISTS requires a SELECT, FROM, PIVOT, or UNPIVOT subquery.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2024_InvalidSubquery,
                Current.Span);

        var subquery = ComposeSetOperators(1);
        Consume(TokenType.RightParenthesis);

        return new ExistsQueryNode(subquery);
    }

    private Node ComposeParenthesizedExpressionOrScalarSubquery()
    {
        Consume(TokenType.LeftParenthesis);

        if (Current.TokenType == TokenType.Select || Current.TokenType == TokenType.From || Current.TokenType == TokenType.Pivot || Current.TokenType == TokenType.Unpivot)
        {
            var subquery = ComposeSetOperators(1);
            Consume(TokenType.RightParenthesis);
            return new ScalarSubqueryNode(subquery);
        }

        var expression = ComposeOperations();
        Consume(TokenType.RightParenthesis);
        return expression;
    }

}
