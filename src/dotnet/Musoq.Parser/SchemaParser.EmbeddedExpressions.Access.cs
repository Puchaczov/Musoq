using System.Collections.Generic;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class SchemaParser
{
    private Node ComposeIdentifierOrFunctionCall()
    {
        var token = ConsumeAndGetToken(Current.TokenType);
        var name = token.Value;

        if (Current.TokenType == TokenType.LeftParenthesis)
        {
            Consume(TokenType.LeftParenthesis);
            var args = new List<Node>();

            if (Current.TokenType != TokenType.RightParenthesis)
            {
                args.Add(ComposeExpression());
                while (Current.TokenType == TokenType.Comma)
                {
                    Consume(TokenType.Comma);
                    args.Add(ComposeExpression());
                }
            }

            Consume(TokenType.RightParenthesis);
            var funcToken = new FunctionToken(name, token.Span);
            return ComposePostfixAccess(new AccessMethodNode(funcToken, new ArgsListNode([..args]), null, false));
        }

        Node result = new IdentifierNode(name);
        return ComposePostfixAccess(result);
    }

    private Node ComposePostfixAccess(Node node)
    {
        while (true)
            if (Current.TokenType == TokenType.Dot)
            {
                Consume(TokenType.Dot);

                if (Current.TokenType is TokenType.LeftParenthesis or TokenType.RightParenthesis
                    or TokenType.LeftSquareBracket or TokenType.RightSquareBracket
                    or TokenType.LBracket or TokenType.RBracket
                    or TokenType.Comma or TokenType.Semicolon
                    or TokenType.Plus or TokenType.Hyphen or TokenType.Star or TokenType.FSlash
                    or TokenType.Mod or TokenType.Equality or TokenType.GreaterEqual
                    or TokenType.LessEqual or TokenType.Greater or TokenType.Less
                    or TokenType.Diff or TokenType.Dot or TokenType.EndOfFile
                    or TokenType.Integer or TokenType.Decimal or TokenType.Function)
                    throw new SyntaxException(
                        $"Expected identifier after '.' but found '{Current.TokenType}' ({Current.Value})",
                        _lexer.AlreadyResolvedQueryPart);

                var memberToken = ConsumeAndGetToken(Current.TokenType);
                var memberNode = new IdentifierNode(memberToken.Value);
                node = new DotNode(node, memberNode, memberToken.Value);
            }
            else if (Current.TokenType == TokenType.LeftSquareBracket)
            {
                Consume(TokenType.LeftSquareBracket);
                var indexExpr = ComposeExpression();
                Consume(TokenType.RightSquareBracket);
                node = new ArrayIndexNode(node, indexExpr);
            }
            else
            {
                break;
            }

        return node;
    }

    private Node? ComposeOptionalAtOffset()
    {
        if (Current.TokenType != TokenType.At)
            return null;

        Consume(TokenType.At);
        return ComposeSizeExpression();
    }

    private FieldConstraintNode? ComposeOptionalConstraint()
    {
        if (Current.TokenType != TokenType.Check)
            return null;

        Consume(TokenType.Check);

        var hasParenthesis = Current.TokenType == TokenType.LeftParenthesis;
        if (hasParenthesis)
            Consume(TokenType.LeftParenthesis);

        var expression = ComposeComparisonExpression();

        if (hasParenthesis)
            Consume(TokenType.RightParenthesis);

        return new FieldConstraintNode(expression);
    }

    private Node? ComposeOptionalWhenCondition()
    {
        if (Current.TokenType != TokenType.When)
            return null;

        Consume(TokenType.When);
        return ComposeComparisonExpression();
    }
}
