using System.Collections.Generic;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private Node ComposeSetOperators(int nestingLevel)
    {
        return ComposeSetOperators(nestingLevel, composeResultModifiers: true);
    }

    private Node ComposeSetOperators(int nestingLevel, bool composeResultModifiers)
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

            var nextSet = ComposeSetOperators(nestingLevel + 1, composeResultModifiers: false);
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

        if (!composeResultModifiers)
            return node;

        var orderBy = ComposeOrderBy() ?? query.OrderBy;
        var skip = ComposeSkip() ?? query.Skip;
        var take = ComposeTake() ?? query.Take;

        if (isSet)
            return AttachResultModifiers((SetOperatorNode)node, orderBy, skip, take);

        var completedQuery = new QueryNode(
            query.Select,
            query.From,
            query.Where,
            query.GroupBy,
            orderBy,
            skip,
            take,
            query.Window,
            query.Qualify,
            default);
        return nestingLevel == 0
            ? new SingleSetNode(completedQuery)
            : completedQuery;
    }

    private static SetOperatorNode AttachResultModifiers(
        SetOperatorNode node,
        OrderByNode? orderBy,
        SkipNode? skip,
        TakeNode? take)
    {
        return node switch
        {
            UnionNode => new UnionNode(
                node.ResultTableName,
                node.Keys,
                node.Left,
                node.Right,
                node.IsNested,
                node.IsTheLastOne,
                orderBy,
                skip,
                take),
            UnionAllNode => new UnionAllNode(
                node.ResultTableName,
                node.Keys,
                node.Left,
                node.Right,
                node.IsNested,
                node.IsTheLastOne,
                orderBy,
                skip,
                take),
            ExceptNode => new ExceptNode(
                node.ResultTableName,
                node.Keys,
                node.Left,
                node.Right,
                node.IsNested,
                node.IsTheLastOne,
                orderBy,
                skip,
                take),
            IntersectNode => new IntersectNode(
                node.ResultTableName,
                node.Keys,
                node.Left,
                node.Right,
                node.IsNested,
                node.IsTheLastOne,
                orderBy,
                skip,
                take),
            _ => throw new NotSupportedException($"Set operator '{node.GetType().Name}' is not supported.")
        };
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
        return Array.IndexOf(SetOperators, currentTokenType) >= 0;
    }

}
