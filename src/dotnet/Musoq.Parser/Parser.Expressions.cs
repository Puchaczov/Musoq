using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private Node ComposeOperations()
    {
        var node = ComposeAndOperators();
        while (Current.TokenType == TokenType.Or)
        {
            Consume(TokenType.Or);
            ThrowIfMissingRightOperand(Previous!.Value);
            node = new OrNode(node, ComposeAndOperators());
        }

        return node;
    }

    private Node ComposeAndOperators()
    {
        var node = ComposeEqualityOperators();
        while (Current.TokenType == TokenType.And)
        {
            Consume(TokenType.And);
            ThrowIfMissingRightOperand(Previous!.Value);
            node = new AndNode(node, ComposeEqualityOperators());
        }

        return node;
    }

    private Node ComposeArithmeticExpression(int minPrecedence, bool allowPostfixCasts = true)
    {
        var left = allowPostfixCasts
            ? ComposePostfixExpression(minPrecedence)
            : ComposeBaseTypes(minPrecedence);
        if (IsNumericToken(Current) && !IsAttachedSignedNumeric(Current))
        {
            if (IsNumericPropertyAccess(Current) ||
                DialectSyntaxFacts.IsUnsupportedNumericPrefix(left, Current, _lexer.AlreadyResolvedQueryPart))
                left = new AddNode(left, ComposePostfixExpression(minPrecedence));
            else
                throw new SyntaxException(
                    "Two operands must be separated by an operator.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ2018_MissingOperator,
                    new TextSpan(Current.Span.Start, 0));
        }

        while (GetArithmeticPrecedence(Current.TokenType) >= minPrecedence ||
               allowPostfixCasts && Current.TokenType == TokenType.DoubleColon)
        {
            if (Current.TokenType == TokenType.DoubleColon)
            {
                left = ComposePostfixExpression(minPrecedence, left);
                continue;
            }

            var curr = Current;
            var precedence = GetArithmeticPrecedence(curr.TokenType);
            var nextMinPrecedence = curr.TokenType == TokenType.NullCoalescing ? precedence : precedence + 1;
            Consume(Current.TokenType);

            if (GetArithmeticPrecedence(Current.TokenType) >= 0 &&
                Current.TokenType is not TokenType.Hyphen and not TokenType.Plus)
                throw new SyntaxException(
                    "An operator cannot be followed by another operator.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ2019_InvalidOperator,
                    Current.Span);

            ThrowIfMissingRightOperand(curr.Value);
            if (curr.TokenType == TokenType.Dot &&
                SqlKeywordTokenFacts.CanRepresentQualifiedIdentifier(Current.TokenType))
                ReplaceCurrentToken(new ColumnToken(Current.Value, Current.Span));

            var right = ComposeArithmeticExpression(nextMinPrecedence, curr.TokenType != TokenType.Dot);
            left = curr.TokenType switch
            {
                TokenType.Plus => new AddNode(left, right),
                TokenType.Hyphen => new HyphenNode(left, right),
                TokenType.Star => new StarNode(left, right),
                TokenType.FSlash => new FSlashNode(left, right),
                TokenType.Mod => new ModuloNode(left, right),
                TokenType.Dot => new DotNode(left, right, string.Empty),
                TokenType.Ampersand => new BitwiseAndNode(left, right),
                TokenType.Pipe => new BitwiseOrNode(left, right),
                TokenType.Caret => new BitwiseXorNode(left, right),
                TokenType.LeftShift => new LeftShiftNode(left, right),
                TokenType.RightShift => new RightShiftNode(left, right),
                TokenType.NullCoalescing => new CoalesceNode(left, right),
                _ => throw new SyntaxException(
                    $"{curr.TokenType} is not supported while parsing expression.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ2001_UnexpectedToken,
                    curr.Span)
            };
        }

        return left;
    }

    private bool IsAttachedSignedNumeric(Token token)
    {
        if (!IsNumericToken(token) || token.Value.Length < 2)
            return false;

        var sign = token.Value[0];
        if (sign is not ('-' or '+') ||
            token.Span.Start >= _lexer.Input.Length ||
            _lexer.Input[token.Span.Start] != sign)
            return false;

        // The lexer may attach a sign to a numeric token.  That is a unary
        // literal only when it follows whitespace, an opening/grouping
        // delimiter, or another operator.  In `1-1`, the second token must
        // remain the right operand of binary subtraction rather than being
        // mistaken for an adjacent signed literal.
        var previousIndex = token.Span.Start - 1;
        if (previousIndex < 0)
            return true;

        var previous = _lexer.Input[previousIndex];
        return char.IsWhiteSpace(previous) || previous is '(' or '[' or ',' or ':' or
            '+' or '-' or '*' or '/' or '%' or '^' or '&' or '|' or '?' or '<' or '>' or '=';
    }

    private bool IsNumericPropertyAccess(Token token)
    {
        return token.Value.Length > 0 && token.Value[0] == '.' ||
            token.Span.Start > 0 && _lexer.Input[token.Span.Start - 1] == '.';
    }

}
