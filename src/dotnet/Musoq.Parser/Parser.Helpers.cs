using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private void ConsumeWhiteSpaces()
    {
        while (Current.TokenType == TokenType.WhiteSpace)
            Consume(TokenType.WhiteSpace);
    }


    private void Consume(TokenType tokenType)
    {
        if (!Current.TokenType.Equals(tokenType))
            throw new SyntaxException(
                $"Expected token is {tokenType} but received {Current.TokenType}.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2001_UnexpectedToken,
                Current.Span);

        Previous = Current;
        _hasReplacedToken = false;
        _lexer.Next();
    }



    private void ConsumeAsColumn(TokenType tokenType)
    {
        if (!Current.TokenType.Equals(tokenType))
            throw new SyntaxException(
                $"Expected token is {tokenType} but received {Current.TokenType}.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2001_UnexpectedToken,
                Current.Span);

        _hasReplacedToken = false;
        if (_lexer is Lexer lexer)
        {
            lexer.NextColumn();
            return;
        }

        _lexer.NextOf(ColumnRegex,
            value => new ColumnToken(value, new TextSpan(_lexer.Position, _lexer.Position + value.Length)));
    }


    private Token ConsumeAndGetToken(TokenType expected)
    {
        var token = Current;
        Consume(expected);
        return token;
    }


    private Token ConsumeAndGetToken()
    {
        return ConsumeAndGetToken(Current.TokenType);
    }


    private TNode ComposeAndSkip<TNode>(Func<Parser, TNode> parserAction, TokenType type)
    {
        var node = Compose(parserAction);
        Consume(type);
        return node;
    }


    private TNode ComposeAndSkipIfPresent<TNode>(Func<Parser, TNode> parserAction, TokenType type)
    {
        var node = Compose(parserAction);
        if (Current.TokenType == type)
            Consume(type);

        return node;
    }


    private TNode Compose<TNode>(Func<Parser, TNode> parserAction)
    {
        ArgumentNullException.ThrowIfNull(parserAction);

        var node = parserAction(this);
        return node;
    }

}
