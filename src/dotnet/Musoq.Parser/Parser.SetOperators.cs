using System.Collections.Generic;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private Node ComposeSetOperators(int nestingLevel)
    {
        var isSet = false;
        var query = ComposeQuery();

        Node node = query;
        while (IsSetOperator(Current.TokenType))
        {
            isSet = true;
            var setOperatorType = Current.TokenType;
            Consume(Current.TokenType);

            var keys = ComposeSetOperatorKeys();

            var nextSet = ComposeSetOperators(nestingLevel + 1);
            var isQuery = nextSet is QueryNode;
            node = setOperatorType switch
            {
                TokenType.Except => new ExceptNode(string.Empty, keys, node, nextSet, nestingLevel != 0, isQuery),
                TokenType.Union => new UnionNode(string.Empty, keys, node, nextSet, nestingLevel != 0, isQuery),
                TokenType.UnionAll => new UnionAllNode(string.Empty, keys, node, nextSet, nestingLevel != 0,
                    isQuery),
                TokenType.Intersect => new IntersectNode(string.Empty, keys, node, nextSet, nestingLevel != 0,
                    isQuery),
                _ => node
            };
        }

        return isSet || nestingLevel > 0 ? node : new SingleSetNode(query);
    }


    private string[] ComposeSetOperatorKeys()
    {
        var keys = new List<string>();

        if (Current.TokenType != TokenType.LeftParenthesis) return keys.ToArray();

        Consume(TokenType.LeftParenthesis);

        if (Current.TokenType == TokenType.RightParenthesis)
        {
            Consume(TokenType.RightParenthesis);
            return keys.ToArray();
        }

        keys.Add(ParsePotentiallyDottedName());
        while (Current.TokenType == TokenType.Comma)
        {
            Consume(TokenType.Comma);
            keys.Add(ParsePotentiallyDottedName());
        }

        Consume(TokenType.RightParenthesis);

        return keys.ToArray();
    }


    private string ParsePotentiallyDottedName()
    {
        var value = Current.Value;
        Consume(Current.TokenType);
        if (Current.TokenType != TokenType.Dot) return value;

        Consume(Current.TokenType);
        value = $"{value}.{Current.Value}";
        Consume(Current.TokenType);
        return value;
    }


    private static bool IsSetOperator(TokenType currentTokenType)
    {
        return SetOperators.Contains(currentTokenType);
    }

}
