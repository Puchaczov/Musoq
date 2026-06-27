using System.Collections.Generic;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private WindowNode? ComposeWindowClause()
    {
        if (Current.TokenType != TokenType.Window) return null;

        Consume(TokenType.Window);

        var definitions = new List<WindowDefinitionNode>();

        do
        {
            if (definitions.Count > 0)
                Consume(TokenType.Comma);

            var nameToken = Current;
            Consume(Current.TokenType);
            var windowName = nameToken.Value;

            Consume(TokenType.As);

            var spec = ComposeWindowSpecification();
            definitions.Add(new WindowDefinitionNode(windowName, spec));
        } while (Current.TokenType == TokenType.Comma);

        return new WindowNode(definitions.ToArray());
    }


    private WindowSpecificationNode ComposeWindowSpecification()
    {
        Consume(TokenType.LeftParenthesis);

        FieldNode[]? partitionFields = null;
        FieldOrderedNode[]? orderByFields = null;

        if (Current.TokenType == TokenType.PartitionBy)
        {
            Consume(TokenType.PartitionBy);
            partitionFields = ComposeWindowPartitionFields();
        }

        if (Current.TokenType == TokenType.OrderBy)
        {
            Consume(TokenType.OrderBy);
            orderByFields = ComposeOrderedFields();
        }

        var frame = ComposeWindowFrame();

        Consume(TokenType.RightParenthesis);

        return new WindowSpecificationNode(partitionFields, orderByFields, frame);
    }


    private FieldNode[] ComposeWindowPartitionFields()
    {
        var fields = new List<FieldNode>();
        var i = 0;

        do
        {
            var fieldExpression = ComposeOperations();
            fields.Add(new FieldNode(fieldExpression, i++, string.Empty));
        } while (Current.TokenType != TokenType.RightParenthesis &&
                 Current.TokenType != TokenType.OrderBy &&
                 !IsContextualKeyword("rows") &&
                 !IsContextualKeyword("range") &&
                 ConsumeAndGetToken().TokenType == TokenType.Comma);

        return fields.ToArray();
    }


    private QualifyNode? ComposeQualify()
    {
        if (!IsContextualKeyword("qualify")) return null;

        Consume(Current.TokenType);

        return new QualifyNode(ComposeOperations());
    }


    private WindowFrameNode? ComposeWindowFrame()
    {
        if (!IsContextualKeyword("rows") && !IsContextualKeyword("range"))
            return null;

        var frameType = IsContextualKeyword("rows")
            ? WindowFrameType.Rows
            : WindowFrameType.Range;

        Consume(Current.TokenType);

        if (Current.Value?.Equals("between", StringComparison.OrdinalIgnoreCase) == true)
        {
            Consume(TokenType.Between);
            var start = ComposeWindowFrameBound();

            if (Current.TokenType != TokenType.And)
                throw new SyntaxException("Expected 'AND' after frame start bound.", _lexer.AlreadyResolvedQueryPart);
            Consume(TokenType.And);

            var end = ComposeWindowFrameBound();
            return new WindowFrameNode(frameType, start, end);
        }

        var singleBound = ComposeWindowFrameBound();
        var implicitEnd = new WindowFrameBoundNode(WindowFrameBoundType.CurrentRow);
        return new WindowFrameNode(frameType, singleBound, implicitEnd);
    }


    private WindowFrameBoundNode ComposeWindowFrameBound()
    {
        if (IsContextualKeyword("unbounded"))
        {
            Consume(Current.TokenType);
            if (IsContextualKeyword("preceding"))
            {
                Consume(Current.TokenType);
                return new WindowFrameBoundNode(WindowFrameBoundType.UnboundedPreceding);
            }
            if (IsContextualKeyword("following"))
            {
                Consume(Current.TokenType);
                return new WindowFrameBoundNode(WindowFrameBoundType.UnboundedFollowing);
            }

            throw new SyntaxException("Expected 'PRECEDING' or 'FOLLOWING' after 'UNBOUNDED'.",
                _lexer.AlreadyResolvedQueryPart);
        }

        if (Current.TokenType == TokenType.CurrentRow)
        {
            Consume(TokenType.CurrentRow);
            return new WindowFrameBoundNode(WindowFrameBoundType.CurrentRow);
        }

        if (Current.TokenType == TokenType.Integer)
        {
            var offset = int.Parse(Current.Value, System.Globalization.CultureInfo.InvariantCulture);
            Consume(TokenType.Integer);

            if (IsContextualKeyword("preceding"))
            {
                Consume(Current.TokenType);
                return new WindowFrameBoundNode(WindowFrameBoundType.OffsetPreceding, offset);
            }
            if (IsContextualKeyword("following"))
            {
                Consume(Current.TokenType);
                return new WindowFrameBoundNode(WindowFrameBoundType.OffsetFollowing, offset);
            }

            throw new SyntaxException("Expected 'PRECEDING' or 'FOLLOWING' after integer offset.",
                _lexer.AlreadyResolvedQueryPart);
        }

        throw new SyntaxException("Expected window frame bound (UNBOUNDED PRECEDING/FOLLOWING, CURRENT ROW, or N PRECEDING/FOLLOWING).",
            _lexer.AlreadyResolvedQueryPart);
    }


    private bool IsContextualKeyword(string keyword)
    {
        return Current.Value?.Equals(keyword, StringComparison.OrdinalIgnoreCase) == true;
    }


    private Node TryComposeWindowFunction(AccessMethodNode methodNode)
    {
        if (Current.TokenType == TokenType.Over)
        {
            Consume(TokenType.Over);

            if (Current.TokenType == TokenType.LeftParenthesis)
            {
                var spec = ComposeWindowSpecification();
                return new WindowFunctionNode(methodNode, spec);
            }

            var windowName = Current.Value;
            Consume(Current.TokenType);
            return new WindowFunctionNode(methodNode, windowName);
        }

        if (Current is FunctionToken { Value: var funcName } && funcName.Equals("over", StringComparison.OrdinalIgnoreCase))
        {
            Consume(TokenType.Function);
            var spec = ComposeWindowSpecification();
            return new WindowFunctionNode(methodNode, spec);
        }

        return methodNode;
    }

}
