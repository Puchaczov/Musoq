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

        var windowToken = ConsumeAndGetToken(TokenType.Window);

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
            definitions.Add((WindowDefinitionNode)new WindowDefinitionNode(windowName, spec)
                .WithSpan(nameToken.Span.Through(spec.Span)));
        } while (Current.TokenType == TokenType.Comma);

        return (WindowNode)new WindowNode(definitions.ToArray())
            .WithSpan(windowToken.Span.Through(definitions[^1].Span));
    }


    private WindowSpecificationNode ComposeWindowSpecification()
    {
        var openingToken = ConsumeAndGetToken(TokenType.LeftParenthesis);

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
            orderByFields = ComposeWindowOrderedFields();
        }

        var frame = ComposeWindowFrame();

        var closingToken = ConsumeAndGetToken(TokenType.RightParenthesis);

        return (WindowSpecificationNode)new WindowSpecificationNode(partitionFields, orderByFields, frame)
            .WithSpan(openingToken.Span.Through(closingToken.Span));
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
            var offsetToken = (IntegerToken)Current;
            int offset;
            try
            {
                offset = int.Parse(offsetToken.Value, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (IsNumericConstructionFailure(ex))
            {
                throw NumericLiteralOutOfRange(offsetToken, ex);
            }

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
                return ((WindowFunctionNode)new WindowFunctionNode(methodNode, spec))
                    .WithSpan(methodNode.Span.Through(spec.Span));
            }

            var windowNameToken = ConsumeAndGetToken(Current.TokenType);
            return ((WindowFunctionNode)new WindowFunctionNode(methodNode, windowNameToken.Value))
                .WithSpan(methodNode.Span.Through(windowNameToken.Span));
        }

        if (Current is FunctionToken { Value: var funcName } && funcName.Equals("over", StringComparison.OrdinalIgnoreCase))
        {
            Consume(TokenType.Function);
            var spec = ComposeWindowSpecification();
            return ((WindowFunctionNode)new WindowFunctionNode(methodNode, spec))
                .WithSpan(methodNode.Span.Through(spec.Span));
        }

        return methodNode;
    }

}
